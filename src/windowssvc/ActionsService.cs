using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Grpc.Net.Client;    // For creating gRPC clients (if needed)
using Microsoft.Extensions.Hosting;  // For BackgroundService
using System.Threading.Tasks;


namespace ArcusWinSvc;

/// <summary>
/// 
/// </summary>
public class ActionsServiceImpl : ActionsService.ActionsServiceBase
{
    public override Task<ListResponse> ExecuteCommand(ListRequest request, ServerCallContext context)
    {
        // This is just temporary implementation
        var result = $"Executed command: {request.Command}";
        return Task.FromResult(new ListResponse { Result = result });
    }
}