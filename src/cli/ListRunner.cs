using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using Arcus.GRPC;

namespace ArcusCli;

/// <summary>
/// Queries the service for the files it maintains
/// </summary>
public class ListRunner(ILogger<ListRunner> logger) : AbstractBaseRunner<ListRunner>(logger)
{
    public override CliCommand Command { get; } = CliCommand.List;

    public override void Run()
    {
        var listRequest = new ListRequest()
        {
            FiltersJson = string.Empty
        };
        
        var reply = client.List(listRequest);
        
        foreach(FileRecord record in reply.Files)
        {
            logger.LogInformation($"File: {record.FileName}");
        }
        
        logger.LogInformation($"Arcus Service reports {reply.Count} files found");
    }
}