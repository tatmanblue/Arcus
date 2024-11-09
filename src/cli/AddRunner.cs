using Microsoft.Extensions.Logging;

namespace ArcusCli;

public class AddRunner : IArgumentRunner
{
    public CliCommand Command { get; } = CliCommand.Add;

    private ILogger<AddRunner> logger;
    private string[] args; 

    public AddRunner(ILogger<AddRunner> logger, string[] args)
    {
        this.logger = logger;
        this.args = args;
    }
    
    public void Run()
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
        
        logger.LogInformation($"adding file {fileName}/'{fullFilePath}'");
    }
}