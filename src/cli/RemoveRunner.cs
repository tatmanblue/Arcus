using Arcus.GRPC;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

public class RemoveRunner(ILogger<RemoveRunner> logger, string[] args)
    : AbstractBaseRunner<RemoveRunner>(logger, args)
{
    public override CliCommand Command { get; } = CliCommand.Remove;
    public override void Run()
    {
        if (null == args || args.Length == 0)
            throw new CliArgumentException();
        
        logger.LogDebug($"Remove arguments: {string.Join(" ", args)}");

        var parser = new ArgumentParser(args);
        
        string id = parser.GetArgument<string>("id");
        
        if (string.IsNullOrEmpty(id))
            throw new CliArgumentException();
        
        var request = new RemoveRequest()
        {
            Id = id,
        };
        
        var response = client.Remove(request);
        
        logger.LogInformation($"Get response: {response}");

    }
}