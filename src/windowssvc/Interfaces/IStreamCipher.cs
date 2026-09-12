namespace ArcusWinSvc.Interfaces;

/// <summary>
/// Wraps a raw file stream so encryption at rest is transparent to callers -- writes are
/// encrypted on the way in, reads are decrypted on the way out. "none" is a first-class
/// implementation of this interface, not a special case, so encryption can be turned on,
/// off, or switched to a different algorithm purely through configuration.
/// </summary>
public interface IStreamCipher
{
    /// <summary>
    /// Identifies this cipher in IndexFileRecord.CipherVersion, e.g. "none", "aes-256-gcm".
    /// </summary>
    string Name { get; }

    Stream WrapForWrite(Stream underlying);

    Stream WrapForRead(Stream underlying);
}
