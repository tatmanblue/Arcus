using System.Security.Cryptography;
using ArcusWinSvc.Interfaces;
using ArcusWinSvc.Security.Ciphers;
using Xunit;

namespace ArcusWinSvc.Tests;

// All tests here read/write ARCUS_ENCRYPTION_ALGORITHM. xUnit runs methods within one class
// sequentially by default, so keeping every env-var-touching test in this single class (and
// restoring the value after each test) avoids cross-test interference.
public class StreamCipherFactoryTests
{
    private const string AlgorithmEnvVar = "ARCUS_ENCRYPTION_ALGORITHM";
    private static readonly byte[] TestKey = RandomNumberGenerator.GetBytes(Aes256GcmStreamCipher.KeySize);

    private class FixedKeyProvider(byte[] key) : IKeyProvider
    {
        public byte[] GetKey() => key;
    }

    private sealed class NeverCalledKeyProvider : IKeyProvider
    {
        public byte[] GetKey() =>
            throw new InvalidOperationException("GetKey should not be called unless a real cipher is actually used.");
    }

    private static void WithAlgorithmEnvVar(string? value, Action test)
    {
        string? original = Environment.GetEnvironmentVariable(AlgorithmEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(AlgorithmEnvVar, value);
            test();
        }
        finally
        {
            Environment.SetEnvironmentVariable(AlgorithmEnvVar, original);
        }
    }

    [Fact]
    public void Default_IsNone_WhenEnvironmentVariableIsUnset()
    {
        WithAlgorithmEnvVar(null, () =>
        {
            var factory = new StreamCipherFactory(new NeverCalledKeyProvider());
            Assert.Equal(NoneStreamCipher.AlgorithmName, factory.Default.Name);
        });
    }

    [Fact]
    public void Default_IsConfiguredAlgorithm_WhenSet()
    {
        WithAlgorithmEnvVar(Aes256GcmStreamCipher.AlgorithmName, () =>
        {
            var factory = new StreamCipherFactory(new FixedKeyProvider(TestKey));
            Assert.Equal(Aes256GcmStreamCipher.AlgorithmName, factory.Default.Name);
        });
    }

    [Fact]
    public void Default_FallsBackToNone_WhenConfiguredAlgorithmIsUnrecognized()
    {
        WithAlgorithmEnvVar("some-typo-d-algorithm-name", () =>
        {
            var factory = new StreamCipherFactory(new NeverCalledKeyProvider());
            Assert.Equal(NoneStreamCipher.AlgorithmName, factory.Default.Name);
        });
    }

    [Fact]
    public void Resolve_ReturnsNone_WhenCipherVersionIsEmpty()
    {
        var factory = new StreamCipherFactory(new NeverCalledKeyProvider());
        Assert.Equal(NoneStreamCipher.AlgorithmName, factory.Resolve(string.Empty).Name);
    }

    [Fact]
    public void Resolve_Throws_ForUnknownCipherVersion()
    {
        var factory = new StreamCipherFactory(new NeverCalledKeyProvider());
        Assert.Throws<InvalidOperationException>(() => factory.Resolve("some-future-cipher-this-build-does-not-know"));
    }
}
