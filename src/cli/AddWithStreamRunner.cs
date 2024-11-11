using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// Sends a file to the service
/// </summary>
/// <param name="logger"></param>
/// <param name="args"></param>
public class AddWithStreamRunner(ILogger<AddWithStreamRunner> logger, string[] args)
    : AbstractBaseRunner<AddWithStreamRunner>(logger, args)
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
        var ftc = new FileTransferClient(client);

        var response = ftc.UploadFileAsync(shortName, fullFilePath).Result;
        logger.LogInformation($"File {shortName} is uploaded. Id: {response.Id}");
    }
}