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

        var parser = new ArgumentParser(args);
        
        string id = parser.GetArgument<string>("id");
        string path = parser.GetArgument<string>("path");
        
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(path))
            throw new CliArgumentException();
        
        string fullFilePath = Path.GetFullPath(path);
        
        if (false == Directory.Exists(fullFilePath))
            throw new CliInvalidInputException("Path does not exist");
        
        var request = new GetRequest()
        {
            Id = id,
            DestinationPath = fullFilePath
        };
        
        var response = client.Get(request);
        
        logger.LogInformation($"Get response: {response}");

    }
}