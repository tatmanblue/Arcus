using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security.Ciphers;

/// <summary>
/// Reads ARCUS_ENCRYPTION_ALGORITHM once at startup to pick the Default cipher for new
/// writes; Resolve looks up any known cipher by name for reads, regardless of what's
/// currently configured, so a record written under one algorithm stays readable even
/// after the configured algorithm changes.
/// </summary>
public class StreamCipherFactory : IStreamCipherFactory
{
    private const string ARCUS_ENCRYPTION_ALGORITHM = "ARCUS_ENCRYPTION_ALGORITHM";

    private readonly Dictionary<string, IStreamCipher> ciphers;

    public StreamCipherFactory(IKeyProvider keyProvider)
    {
        var none = new NoneStreamCipher();
        var aesGcm = new Aes256GcmStreamCipher(keyProvider);

        ciphers = new Dictionary<string, IStreamCipher>(StringComparer.OrdinalIgnoreCase)
        {
            [none.Name] = none,
            [aesGcm.Name] = aesGcm
        };

        string configured = Environment.GetEnvironmentVariable(ARCUS_ENCRYPTION_ALGORITHM) ?? NoneStreamCipher.AlgorithmName;
        Default = ciphers.TryGetValue(configured, out IStreamCipher? configuredCipher) ? configuredCipher : none;
    }

    public IStreamCipher Default { get; }

    public IStreamCipher Resolve(string cipherVersion)
    {
        if (string.IsNullOrEmpty(cipherVersion))
            return ciphers[NoneStreamCipher.AlgorithmName];

        if (ciphers.TryGetValue(cipherVersion, out IStreamCipher? cipher))
            return cipher;

        throw new InvalidOperationException(
            $"Record was written with cipher '{cipherVersion}', which this build does not know how to read.");
    }
}
