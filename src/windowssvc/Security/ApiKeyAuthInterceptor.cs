using ArcusWinSvc.Interfaces;
using Grpc.Core;
using Grpc.Core.Interceptors;
using IConfiguration = ArcusWinSvc.Interfaces.IConfiguration;

namespace ArcusWinSvc.Security;

/// <summary>
/// Rejects any gRPC call that doesn't present a valid API key, when API keys are
/// configured (ARCUS_API_KEYS). Built around a *list* of valid keys -- one per client --
/// so a single compromised/retired client can be revoked later without affecting anyone
/// else, and without redesigning this class. When no keys are configured, enforcement is
/// off entirely -- matches every other Phase A security control being opt-in, and mirrors
/// IronBar's own MCP server precedent (IRONBAR_MCP_ACCESS_TOKENS).
///
/// Covers all four gRPC call shapes (unary, client/server/duplex streaming) even though
/// Arcus only uses three of them today -- this is a security boundary, not a feature, so
/// it shouldn't have a gap waiting for whoever adds a duplex RPC later to remember to plug.
/// </summary>
public class ApiKeyAuthInterceptor : Interceptor
{
    public const string MetadataKey = "x-api-key";

    private readonly HashSet<string> validKeys;

    public ApiKeyAuthInterceptor(IConfiguration config)
    {
        validKeys = new HashSet<string>(config.ApiKeys);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        return await continuation(request, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        return await continuation(requestStream, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        await continuation(request, responseStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthorized(context);
        await continuation(requestStream, responseStream, context);
    }

    private void EnsureAuthorized(ServerCallContext context)
    {
        if (validKeys.Count == 0)
            return;

        Metadata.Entry? entry = context.RequestHeaders?.Get(MetadataKey);
        if (entry is null || !validKeys.Contains(entry.Value))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing or invalid API key."));
    }
}
