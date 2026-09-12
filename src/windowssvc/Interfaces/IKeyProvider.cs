namespace ArcusWinSvc.Interfaces;

/// <summary>
/// Supplies the symmetric key a keyed IStreamCipher encrypts/decrypts with. Kept separate
/// from IStreamCipher so "where the key lives" and "how encryption is done" can vary
/// independently -- a client-held key provider is how client-side encryption could be
/// added later without redesigning the cipher itself.
/// </summary>
public interface IKeyProvider
{
    byte[] GetKey();
}
