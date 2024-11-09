using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using Arcus.GRPC;

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
        
        // TODO: gotta output the results
        
        logger.LogInformation($"Arcus Service reports {reply.Count} files found");
    }
}