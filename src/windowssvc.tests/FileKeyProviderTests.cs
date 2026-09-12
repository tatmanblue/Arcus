using System.Security.Cryptography;
using ArcusWinSvc.Security;
using Xunit;

namespace ArcusWinSvc.Tests;

public class FileKeyProviderTests
{
    private const string KeyFileEnvVar = "ARCUS_ENCRYPTION_KEY_FILE";

    private static void WithKeyFileEnvVar(string? value, Action test)
    {
        string? original = Environment.GetEnvironmentVariable(KeyFileEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(KeyFileEnvVar, value);
            test();
        }
        finally
        {
            Environment.SetEnvironmentVariable(KeyFileEnvVar, original);
        }
    }

    [Fact]
    public void GetKey_ReturnsFileContents()
    {
        string tempFile = Path.GetTempFileName();
        byte[] keyBytes = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(tempFile, keyBytes);

        try
        {
            WithKeyFileEnvVar(tempFile, () =>
            {
                var provider = new FileKeyProvider();
                Assert.Equal(keyBytes, provider.GetKey());
            });
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetKey_Throws_WhenEnvironmentVariableIsUnset()
    {
        WithKeyFileEnvVar(null, () =>
        {
            var provider = new FileKeyProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetKey());
        });
    }

    [Fact]
    public void GetKey_Throws_WhenFileDoesNotExist()
    {
        WithKeyFileEnvVar(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".key"), () =>
        {
            var provider = new FileKeyProvider();
            Assert.Throws<FileNotFoundException>(() => provider.GetKey());
        });
    }
}
