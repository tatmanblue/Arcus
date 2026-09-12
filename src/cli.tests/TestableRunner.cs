using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace ArcusCli.Tests;

/// <summary>
/// Minimal concrete AbstractBaseRunner so tests can construct one and inspect the channel
/// it builds, without depending on any of the real runners' command-specific behavior.
/// </summary>
public class TestableRunner(ILogger<TestableRunner> logger) : AbstractBaseRunner<TestableRunner>(logger, [])
{
    public override CliCommand Command => CliCommand.Help;

    public override void Run()
    {
    }

    public GrpcChannel ExposedChannel => channel;
}
