using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArcusCli.Tests;

// Touches ARCUS_SERVICE_URL; xUnit runs methods within a class sequentially by default,
// and the value is restored after each test to avoid leaking into other test classes.
public class AbstractBaseRunnerTests
{
    private const string ServiceUrlEnvVar = "ARCUS_SERVICE_URL";

    private static void WithServiceUrlEnvVar(string? value, Action test)
    {
        string? original = Environment.GetEnvironmentVariable(ServiceUrlEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ServiceUrlEnvVar, value);
            test();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ServiceUrlEnvVar, original);
        }
    }

    [Fact]
    public void Channel_UsesDefaultUrl_WhenEnvironmentVariableIsUnset()
    {
        WithServiceUrlEnvVar(null, () =>
        {
            using var runner = new TestableRunner(NullLogger<TestableRunner>.Instance);

            Assert.Contains("localhost:5001", runner.ExposedChannel.Target);
        });
    }

    [Fact]
    public void Channel_UsesConfiguredUrl_WhenEnvironmentVariableIsSet()
    {
        WithServiceUrlEnvVar("http://myhost:9999", () =>
        {
            using var runner = new TestableRunner(NullLogger<TestableRunner>.Instance);

            Assert.Contains("myhost:9999", runner.ExposedChannel.Target);
        });
    }
}
