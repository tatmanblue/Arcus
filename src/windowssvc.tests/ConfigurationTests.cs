using Xunit;

namespace ArcusWinSvc.Tests;

// Touches ARCUS_GPRC_PORT/ARCUS_GRPC_MSG_SIZE; xUnit runs methods within a class
// sequentially by default, and values are restored after each test.
public class ConfigurationTests
{
    private const string PortEnvVar = "ARCUS_GPRC_PORT";
    private const string MessageSizeEnvVar = "ARCUS_GRPC_MSG_SIZE";

    private static void WithEnvVars(string? port, string? messageSize, Action test)
    {
        string? originalPort = Environment.GetEnvironmentVariable(PortEnvVar);
        string? originalSize = Environment.GetEnvironmentVariable(MessageSizeEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(PortEnvVar, port);
            Environment.SetEnvironmentVariable(MessageSizeEnvVar, messageSize);
            test();
        }
        finally
        {
            Environment.SetEnvironmentVariable(PortEnvVar, originalPort);
            Environment.SetEnvironmentVariable(MessageSizeEnvVar, originalSize);
        }
    }

    [Fact]
    public void GrpcPort_UsesDefault_WhenEnvironmentVariableIsUnset()
    {
        WithEnvVars(null, null, () =>
        {
            var config = new Configuration();
            Assert.Equal(5001, config.GrpcPort);
        });
    }

    [Fact]
    public void GrpcPort_UsesConfiguredValue_WhenSetWithinRange()
    {
        WithEnvVars("9999", null, () =>
        {
            var config = new Configuration();
            Assert.Equal(9999, config.GrpcPort);
        });
    }

    [Fact]
    public void GrpcPort_FallsBackToDefault_WhenConfiguredValueIsOutOfRange()
    {
        WithEnvVars("80", null, () =>
        {
            var config = new Configuration();
            Assert.Equal(5001, config.GrpcPort);
        });
    }

    [Fact]
    public void GrpcMaxMessageSize_UsesDefault_WhenEnvironmentVariableIsUnset()
    {
        WithEnvVars(null, null, () =>
        {
            var config = new Configuration();
            Assert.Equal(10 * 1024, config.GrpcMaxMessageSize);
        });
    }

    [Fact]
    public void GrpcMaxMessageSize_UsesConfiguredValue_WhenSetAboveMinimum()
    {
        WithEnvVars(null, "20000", () =>
        {
            var config = new Configuration();
            Assert.Equal(20000, config.GrpcMaxMessageSize);
        });
    }
}
