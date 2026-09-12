using System.Security.Cryptography;

namespace ArcusWinSvc.Security.Ciphers;

/// <summary>
/// Reverses Aes256GcmEncryptingStream's framing: reads one
/// [4-byte length][12-byte nonce][ciphertext][16-byte tag] record at a time, decrypts it,
/// and serves the plaintext out through Read -- one chunk of plaintext buffered at a time,
/// never the whole file in memory.
/// </summary>
internal sealed class Aes256GcmDecryptingStream : Stream
{
    private readonly Stream underlying;
    private readonly byte[] key;
    private byte[] pendingPlaintext = Array.Empty<byte>();
    private int pendingOffset;
    private bool endOfStream;

    public Aes256GcmDecryptingStream(Stream underlying, byte[] key)
    {
        this.underlying = underlying;
        this.key = key;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (pendingOffset >= pendingPlaintext.Length && !endOfStream)
            FillNextChunk();

        int available = pendingPlaintext.Length - pendingOffset;
        if (available <= 0)
            return 0;

        int toCopy = Math.Min(available, count);
        Buffer.BlockCopy(pendingPlaintext, pendingOffset, buffer, offset, toCopy);
        pendingOffset += toCopy;
        return toCopy;
    }

    private void FillNextChunk()
    {
        byte[] lengthPrefix = ReadExact(4);
        if (lengthPrefix.Length == 0)
        {
            endOfStream = true;
            pendingPlaintext = Array.Empty<byte>();
            pendingOffset = 0;
            return;
        }

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix);
        int plaintextLength = BitConverter.ToInt32(lengthPrefix, 0);

        byte[] nonce = ReadExact(Aes256GcmStreamCipher.NonceSize);
        byte[] ciphertext = ReadExact(plaintextLength);
        byte[] tag = ReadExact(Aes256GcmStreamCipher.TagSize);

        byte[] plaintext = new byte[plaintextLength];
        using (var aesGcm = new AesGcm(key, Aes256GcmStreamCipher.TagSize))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        pendingPlaintext = plaintext;
        pendingOffset = 0;
    }

    private byte[] ReadExact(int length)
    {
        if (length == 0)
            return Array.Empty<byte>();

        byte[] buffer = new byte[length];
        int totalRead = 0;
        while (totalRead < length)
        {
            int read = underlying.Read(buffer, totalRead, length - totalRead);
            if (read == 0)
            {
                if (totalRead == 0)
                    return Array.Empty<byte>(); // clean end-of-stream between chunks

                throw new EndOfStreamException("Encrypted file ended mid-chunk -- it is truncated or corrupted.");
            }

            totalRead += read;
        }

        return buffer;
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            underlying.Dispose();

        base.Dispose(disposing);
    }
}
