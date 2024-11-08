using Microsoft.Extensions.Logging;
using Grpc.Net.Client;

namespace ArcusCli;
public class ListRunner : IArgumentRunner
{
    private ILogger<ListRunner> logger;
    
    public CliCommand Command { get; } = CliCommand.List;

    public ListRunner(ILogger<ListRunner> logger)
    {
        this.logger = logger;
    }
    
    public void Run()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5001");
        var client = new ActionsService.ActionsServiceClient(channel);
        var listRequest = new ListRequest()
        {
            FiltersJson = string.Empty
        };
        
        var reply = client.List(listRequest);
        logger.LogInformation("Arcus Service says: " + reply.Count);
    }
}