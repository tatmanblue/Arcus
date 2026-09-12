using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security;

/// <summary>
/// Default IKeyProvider: reads the encryption key from a file path given by
/// ARCUS_ENCRYPTION_KEY_FILE. Works identically on every OS and is easy for a user to
/// understand, back up, and rotate manually. Guidance (not enforced here): keep this path
/// outside the vault's own StoreLocation so the key isn't swept up in whatever backs up or
/// copies the data directory itself. Only read when a real cipher actually asks for a key
/// -- running with the "none" cipher never touches this, so an unset/missing key file is
/// not an error unless encryption is actually turned on.
/// </summary>
public class FileKeyProvider : IKeyProvider
{
    private const string ARCUS_ENCRYPTION_KEY_FILE = "ARCUS_ENCRYPTION_KEY_FILE";

    private readonly Lazy<byte[]> key = new(LoadKey);

    public byte[] GetKey() => key.Value;

    private static byte[] LoadKey()
    {
        string? path = Environment.GetEnvironmentVariable(ARCUS_ENCRYPTION_KEY_FILE);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException(
                $"{ARCUS_ENCRYPTION_KEY_FILE} must be set to use an encryption algorithm other than 'none'.");

        if (!File.Exists(path))
            throw new FileNotFoundException("Configured encryption key file was not found.", path);

        return File.ReadAllBytes(path);
    }
}
