using Microsoft.Extensions.Logging;
using Arcus.GRPC;

namespace ArcusCli;

/// <summary>
/// AddRunner gives file information to the service for it to store
/// </summary>
public class AddRunner(ILogger<AddRunner> logger, string[] args) 
    : AbstractBaseRunner<AddRunner>(logger, args)
{
    public override CliCommand Command { get; } = CliCommand.Add;

    public override void Run()
    {
        logger.LogDebug("Running add");
        
        if (null == args || args.Length == 0)
            throw new CliArgumentException();
        
        logger.LogDebug($"Add arguments: {string.Join(" ", args)}");
        
        var parser = new ArgumentParser(args);
        
        string fileName = parser.GetArgument<string>("file");
        if (string.IsNullOrEmpty(fileName))
            throw new CliArgumentException();

        if (!File.Exists(fileName))
            throw new CliInvalidInputException($"file {fileName} not found");
        
        string fullFilePath = Path.GetFullPath(fileName);
        string shortName = Path.GetFileName(fullFilePath);
        
        var request = new AddRequest()
        {
            ShortName = shortName,
            OriginFullPath = fullFilePath,
        };
        
        var response = client.Add(request);
        
        logger.LogInformation($"File {shortName} is added. Id = {response.Id}");
    }
}