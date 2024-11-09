using Microsoft.Extensions.Logging;
using Arcus.GRPC;

namespace ArcusCli;

public class AddRunner : AbstractBaseRunner<AddRunner>
{
    public override CliCommand Command { get; } = CliCommand.Add;
    
    private string[] args; 

    public AddRunner(ILogger<AddRunner> logger, string[] args) : base(logger)
    {
        this.logger = logger;
        this.args = args;
    }
    
    public override void Run()
    {
        logger.LogDebug("Running add");
        
        if (null == args || args.Length == 0)
            throw new CliArgumentException();
        
        logger.LogDebug($"Add arguments: {string.Join(" ", args)}");
        string fileName = args[0];
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