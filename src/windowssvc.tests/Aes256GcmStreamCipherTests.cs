using System.Security.Cryptography;
using System.Text;
using ArcusWinSvc.Interfaces;
using ArcusWinSvc.Security.Ciphers;
using ArcusWinSvc.Tests.Fakes;
using Xunit;

namespace ArcusWinSvc.Tests;

public class Aes256GcmStreamCipherTests
{
    private static readonly byte[] TestKey = RandomNumberGenerator.GetBytes(Aes256GcmStreamCipher.KeySize);

    private class FixedKeyProvider(byte[] key) : IKeyProvider
    {
        public byte[] GetKey() => key;
    }

    private static byte[] EncryptToBytes(IStreamCipher cipher, byte[] plaintext)
    {
        using var backing = new MemoryStream();
        using (Stream writeStream = cipher.WrapForWrite(new NonDisposingStream(backing)))
        {
            writeStream.Write(plaintext, 0, plaintext.Length);
        }

        return backing.ToArray();
    }

    private static byte[] DecryptFromBytes(IStreamCipher cipher, byte[] encrypted)
    {
        using var backing = new MemoryStream(encrypted);
        using Stream readStream = cipher.WrapForRead(new NonDisposingStream(backing));
        using var output = new MemoryStream();
        readStream.CopyTo(output);
        return output.ToArray();
    }

    private static bool ContainsSubsequence(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0)
            return true;

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

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(Aes256GcmStreamCipher.ChunkSize)]
    [InlineData(Aes256GcmStreamCipher.ChunkSize + 100)]
    [InlineData(Aes256GcmStreamCipher.ChunkSize * 2 + 37)]
    public void RoundTrip_ReturnsOriginalPlaintext(int contentLength)
    {
        var cipher = new Aes256GcmStreamCipher(new FixedKeyProvider(TestKey));
        byte[] plaintext = RandomNumberGenerator.GetBytes(contentLength);

        byte[] encrypted = EncryptToBytes(cipher, plaintext);
        byte[] decrypted = DecryptFromBytes(cipher, encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypted_Bytes_DoNotContainPlaintext()
    {
        var cipher = new Aes256GcmStreamCipher(new FixedKeyProvider(TestKey));
        byte[] plaintext = Encoding.UTF8.GetBytes("this must not appear anywhere in the encrypted output");

        byte[] encrypted = EncryptToBytes(cipher, plaintext);

        Assert.False(ContainsSubsequence(encrypted, plaintext));
    }

    [Fact]
    public void WrapForWrite_Throws_WhenKeyIsWrongSize()
    {
        var cipher = new Aes256GcmStreamCipher(new FixedKeyProvider(new byte[16])); // AES-128 size, not 256
        using var backing = new MemoryStream();

        Assert.Throws<InvalidOperationException>(() => cipher.WrapForWrite(new NonDisposingStream(backing)));
    }

    [Fact]
    public void Decrypt_Throws_WhenCiphertextIsTruncated()
    {
        var cipher = new Aes256GcmStreamCipher(new FixedKeyProvider(TestKey));
        byte[] plaintext = Encoding.UTF8.GetBytes("some content to truncate after encrypting");
        byte[] encrypted = EncryptToBytes(cipher, plaintext);

        byte[] truncated = encrypted[..(encrypted.Length - 5)];

        Assert.ThrowsAny<Exception>(() => DecryptFromBytes(cipher, truncated));
    }
}
