using System.Security.Cryptography;
using System.Text;
using ArcusWinSvc.Security;
using ArcusWinSvc.Security.Ciphers;
using ArcusWinSvc.Tests.Fakes;
using Xunit;

namespace ArcusWinSvc.Tests;

// Touches ARCUS_ENCRYPTION_ALGORITHM/ARCUS_ENCRYPTION_KEY_FILE; kept in one class (xUnit runs
// methods within a class sequentially by default) and restored per-test via Dispose.
public class LocalDataAccessEncryptionTests : IDisposable
{
    private const string AlgorithmEnvVar = "ARCUS_ENCRYPTION_ALGORITHM";
    private const string KeyFileEnvVar = "ARCUS_ENCRYPTION_KEY_FILE";

    private readonly string tempDir;
    private readonly string keyFile;
    private readonly string? originalAlgorithm;
    private readonly string? originalKeyFile;

    public LocalDataAccessEncryptionTests()
    {
        tempDir = Directory.CreateTempSubdirectory("arcus-test-").FullName;
        keyFile = Path.Combine(tempDir, "test.key");
        File.WriteAllBytes(keyFile, RandomNumberGenerator.GetBytes(Aes256GcmStreamCipher.KeySize));

        originalAlgorithm = Environment.GetEnvironmentVariable(AlgorithmEnvVar);
        originalKeyFile = Environment.GetEnvironmentVariable(KeyFileEnvVar);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AlgorithmEnvVar, originalAlgorithm);
        Environment.SetEnvironmentVariable(KeyFileEnvVar, originalKeyFile);
        Directory.Delete(tempDir, recursive: true);
    }

    private static bool ContainsSubsequence(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            int j = 0;
            while (j < needle.Length && haystack[i + j] == needle[j])
                j++;

            if (j == needle.Length)
                return true;
        }

        return false;
    }

    private static async Task<byte[]> ReadAllAsync(ArcusWinSvc.Interfaces.IFileAccessStream stream)
    {
        var buffer = new byte[1024];
        using var output = new MemoryStream();
        int bytesRead;
        while ((bytesRead = await stream.ReadBytes(buffer, buffer.Length)) > 0)
            output.Write(buffer, 0, bytesRead);

        return output.ToArray();
    }

    [Fact]
    public async Task RoundTrip_WithAesGcmConfigured_MatchesPlaintextAndIsNotStoredAsPlaintext()
    {
        Environment.SetEnvironmentVariable(AlgorithmEnvVar, Aes256GcmStreamCipher.AlgorithmName);
        Environment.SetEnvironmentVariable(KeyFileEnvVar, keyFile);

        var config = new FakeConfiguration { StoreLocation = tempDir };
        var cipherFactory = new StreamCipherFactory(new FileKeyProvider());
        var dataAccess = new LocalDataAccess(config, new NoopFileOperations(), cipherFactory);

        var record = new IndexFileRecord { Id = "test-record-id" };
        byte[] plaintext = Encoding.UTF8.GetBytes("this is the content that should be encrypted at rest");

        using (var writeStream = dataAccess.AddRequest(record))
        {
            await writeStream.WriteBytes(plaintext);
        }

        Assert.Equal(Aes256GcmStreamCipher.AlgorithmName, record.CipherVersion);

        string storedFile = Path.Combine(tempDir, record.Id, $"{record.Id}.file");
        byte[] onDiskBytes = await File.ReadAllBytesAsync(storedFile);
        Assert.False(ContainsSubsequence(onDiskBytes, plaintext));

        using var readStream = dataAccess.GetRequest(record);
        byte[] roundTripped = await ReadAllAsync(readStream);

        Assert.Equal(plaintext, roundTripped);
    }

    [Fact]
    public async Task Read_UsesRecordsOwnCipher_EvenAfterConfiguredAlgorithmChanges()
    {
        Environment.SetEnvironmentVariable(AlgorithmEnvVar, Aes256GcmStreamCipher.AlgorithmName);
        Environment.SetEnvironmentVariable(KeyFileEnvVar, keyFile);

        var config = new FakeConfiguration { StoreLocation = tempDir };
        var writeAccess = new LocalDataAccess(config, new NoopFileOperations(), new StreamCipherFactory(new FileKeyProvider()));

        var record = new IndexFileRecord { Id = "changed-config-record" };
        byte[] plaintext = Encoding.UTF8.GetBytes("written while aes-256-gcm was configured");

        using (var writeStream = writeAccess.AddRequest(record))
        {
            await writeStream.WriteBytes(plaintext);
        }

        Assert.Equal(Aes256GcmStreamCipher.AlgorithmName, record.CipherVersion);

        // Flip the configured algorithm back to "none" and read with a fresh factory that
        // reflects that changed configuration. The record still says aes-256-gcm, so reading
        // must use that, not whatever the new default is.
        Environment.SetEnvironmentVariable(AlgorithmEnvVar, NoneStreamCipher.AlgorithmName);
        var readAccess = new LocalDataAccess(config, new NoopFileOperations(), new StreamCipherFactory(new FileKeyProvider()));

        using var readStream = readAccess.GetRequest(record);
        byte[] roundTripped = await ReadAllAsync(readStream);

        Assert.Equal(plaintext, roundTripped);
    }

    [Fact]
    public async Task RoundTrip_WithNoneConfigured_StoresPlaintextUnchanged()
    {
        Environment.SetEnvironmentVariable(AlgorithmEnvVar, NoneStreamCipher.AlgorithmName);

        var config = new FakeConfiguration { StoreLocation = tempDir };
        var dataAccess = new LocalDataAccess(config, new NoopFileOperations(), new StreamCipherFactory(new FileKeyProvider()));

        var record = new IndexFileRecord { Id = "plaintext-record-id" };
        byte[] plaintext = Encoding.UTF8.GetBytes("no encryption configured, should be stored as-is");

        using (var writeStream = dataAccess.AddRequest(record))
        {
            await writeStream.WriteBytes(plaintext);
        }

        Assert.Equal(NoneStreamCipher.AlgorithmName, record.CipherVersion);

        string storedFile = Path.Combine(tempDir, record.Id, $"{record.Id}.file");
        byte[] onDiskBytes = await File.ReadAllBytesAsync(storedFile);
        Assert.Equal(plaintext, onDiskBytes);
    }
}
