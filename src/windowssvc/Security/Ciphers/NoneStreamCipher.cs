using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security.Ciphers;

/// <summary>
/// The default cipher: a pure passthrough. Encryption at rest is off until a deployment
/// explicitly configures a real algorithm.
/// </summary>
public class NoneStreamCipher : IStreamCipher
{
    public const string AlgorithmName = "none";

    public string Name => AlgorithmName;

    public Stream WrapForWrite(Stream underlying) => underlying;

    public Stream WrapForRead(Stream underlying) => underlying;
}
