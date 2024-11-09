using Arcus.GRPC;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

public class GetRunner(ILogger<GetRunner> logger, string[] args) : 
    AbstractBaseRunner<GetRunner>(logger, args)
{
    public override CliCommand Command { get; } = CliCommand.Get;

    public override void Run()
    {
        if (null == args || args.Length == 0)
            throw new CliArgumentException();
        
        logger.LogDebug($"Get arguments: {string.Join(" ", args)}");
        string id = args[0];

        var request = new GetRequest()
        {
            Id = id
        };
        
        var response = client.Get(request);
        
        logger.LogInformation($"Get response: {response}");

    }
}