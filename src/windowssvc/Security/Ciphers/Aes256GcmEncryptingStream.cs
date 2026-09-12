using System.Security.Cryptography;

namespace ArcusWinSvc.Security.Ciphers;

/// <summary>
/// Buffers plaintext into fixed-size chunks and AES-256-GCM-encrypts each one as it fills,
/// framed as [4-byte big-endian plaintext length][12-byte nonce][ciphertext][16-byte tag]
/// so a decrypting reader knows exactly where one chunk ends and the next begins. Each
/// chunk gets its own random nonce -- with a 96-bit nonce the collision risk across the
/// number of chunks a single file could ever contain is negligible, so no counter/state
/// needs to be tracked across writes.
/// </summary>
internal sealed class Aes256GcmEncryptingStream : Stream
{
    private readonly Stream underlying;
    private readonly byte[] key;
    private readonly byte[] chunkBuffer = new byte[Aes256GcmStreamCipher.ChunkSize];
    private int buffered;
    private bool finalized;

    public Aes256GcmEncryptingStream(Stream underlying, byte[] key)
    {
        this.underlying = underlying;
        this.key = key;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        int remaining = count;
        int sourceOffset = offset;

        while (remaining > 0)
        {
            int spaceInChunk = chunkBuffer.Length - buffered;
            int toCopy = Math.Min(spaceInChunk, remaining);
            Buffer.BlockCopy(buffer, sourceOffset, chunkBuffer, buffered, toCopy);
            buffered += toCopy;
            sourceOffset += toCopy;
            remaining -= toCopy;

            if (buffered == chunkBuffer.Length)
                FlushChunk();
        }
    }

    private void FlushChunk()
    {
        if (buffered == 0)
            return;

        byte[] plaintext = new byte[buffered];
        Buffer.BlockCopy(chunkBuffer, 0, plaintext, 0, buffered);

        byte[] nonce = RandomNumberGenerator.GetBytes(Aes256GcmStreamCipher.NonceSize);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[Aes256GcmStreamCipher.TagSize];

        using (var aesGcm = new AesGcm(key, Aes256GcmStreamCipher.TagSize))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        byte[] lengthPrefix = BitConverter.GetBytes(plaintext.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix);

        underlying.Write(lengthPrefix, 0, lengthPrefix.Length);
        underlying.Write(nonce, 0, nonce.Length);
        underlying.Write(ciphertext, 0, ciphertext.Length);
        underlying.Write(tag, 0, tag.Length);

        buffered = 0;
    }

    public override void Flush()
    {
        // Deliberately not flushing a partial chunk here -- Flush() can be called mid-write
        // by unrelated code and doesn't mean "this is the last byte"; only Dispose does.
        underlying.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !finalized)
        {
            FlushChunk();
            underlying.Dispose();
            finalized = true;
        }

        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
