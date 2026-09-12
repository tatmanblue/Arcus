using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security.Ciphers;

/// <summary>
/// AES-256-GCM encryption at rest. AesGcm has no native streaming mode -- it encrypts a
/// whole buffer at a time -- so the actual chunking/framing lives in
/// Aes256GcmEncryptingStream/Aes256GcmDecryptingStream; this class just wires the
/// configured key in and exposes the IStreamCipher contract.
/// </summary>
public class Aes256GcmStreamCipher : IStreamCipher
{
    public const string AlgorithmName = "aes-256-gcm";

    /// <summary>Plaintext bytes per chunk before a new nonce/tag is generated.</summary>
    public const int ChunkSize = 64 * 1024;

    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int KeySize = 32; // AES-256

    private readonly IKeyProvider keyProvider;

    public Aes256GcmStreamCipher(IKeyProvider keyProvider)
    {
        this.keyProvider = keyProvider;
    }

    public string Name => AlgorithmName;

    public Stream WrapForWrite(Stream underlying) =>
        new Aes256GcmEncryptingStream(underlying, GetValidatedKey());

    public Stream WrapForRead(Stream underlying) =>
        new Aes256GcmDecryptingStream(underlying, GetValidatedKey());

    private byte[] GetValidatedKey()
    {
        byte[] key = keyProvider.GetKey();
        if (key.Length != KeySize)
            throw new InvalidOperationException(
                $"{AlgorithmName} requires a {KeySize}-byte key; the configured key is {key.Length} bytes.");

        return key;
    }
}
