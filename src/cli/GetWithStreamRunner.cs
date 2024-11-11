using Arcus.GRPC;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// Gets a files from the service, copying the bytes received to the file specified
/// </summary>
/// <param name="logger"></param>
/// <param name="args"></param>
public class GetWithStreamRunner(ILogger<GetWithStreamRunner> logger, string[] args)
    : AbstractBaseRunner<GetWithStreamRunner>(logger, args)
{
    public override CliCommand Command { get; } = CliCommand.Get;
    public override void Run()
    {
        if (null == args || args.Length == 0)
            throw new CliArgumentException();
        
        logger.LogDebug($"Get arguments: {string.Join(" ", args)}");

        var parser = new ArgumentParser(args);
        
        string id = parser.GetArgument<string>("id");
        string path = parser.GetArgument<string>("file");
        
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(path))
            throw new CliArgumentException();
        
        string fullFilePath = Path.GetFullPath(path);
        
        if (true == File.Exists(fullFilePath))
            throw new CliInvalidInputException("File already exists, overwrite not supported");
        
        var ftc = new FileTransferClient(client);

        long bytes = ftc.DownloadFileAsync(id, fullFilePath).Result;

        logger.LogInformation($"file {id} has been downloaded to {fullFilePath}.  Bytes: {bytes}");
    }
}