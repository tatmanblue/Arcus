using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Arcus.GRPC;


namespace ArcusWinSvc;

/// <summary>
///  GRPC interface for receiving commands from other processes like the ArcusCLI
/// </summary>
public class ActionsServiceImpl : ActionsService.ActionsServiceBase
{
    public override Task<ListResponse> List(ListRequest request, ServerCallContext context)
    {
        // This is just temporary implementation        
        return Task.FromResult(new ListResponse { Count = 0 });
    }
}