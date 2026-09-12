namespace ArcusWinSvc.Interfaces;

/// <summary>
/// Resolves which IStreamCipher to use. New writes always use Default (the currently
/// configured algorithm); reads always use Resolve(record.CipherVersion) -- the cipher a
/// specific record was actually written with -- so changing the configured algorithm can
/// never break previously stored files.
/// </summary>
public interface IStreamCipherFactory
{
    /// <summary>The cipher new files are written with, per ARCUS_ENCRYPTION_ALGORITHM.</summary>
    IStreamCipher Default { get; }

    /// <summary>Looks up the cipher identified by name (typically a record's CipherVersion).</summary>
    IStreamCipher Resolve(string cipherVersion);
}
