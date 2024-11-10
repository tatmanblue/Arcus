using System.Text;
using Microsoft.Extensions.Logging;
using Arcus.GRPC;

namespace ArcusCli;

/// <summary>
/// Queries the service for the files it maintains
/// </summary>
public class ListRunner(ILogger<ListRunner> logger) : AbstractBaseRunner<ListRunner>(logger, [])
{
    public override CliCommand Command { get; } = CliCommand.List;

    public override void Run()
    {
        var listRequest = new ListRequest()
        {
            FiltersJson = string.Empty
        };
        
        var reply = client.List(listRequest);
        
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Arcus Service reports {reply.Count} files found");
        builder.AppendLine($"{"Id", -10} {"Name", -20}");
        builder.AppendLine($"-------    ---------------------------------------------");
        
        foreach(FileRecord record in reply.Files)
        {
            builder.AppendLine($"{record.Id, -10} {record.FileName, -20}");
        }
        
        logger.LogInformation(builder.ToString());
    }
}