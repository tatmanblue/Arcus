using Grpc.Core;
using Grpc.Core.Interceptors;

namespace ArcusCli;

/// <summary>
/// Attaches the configured API key (ARCUS_API_KEY) to every outgoing call, mirroring the
/// server's ApiKeyAuthInterceptor. A no-op when no key is configured -- matches the
/// server's own "off unless configured" behavior rather than requiring a key to be set.
///
/// Covers all five client call shapes even though the current runners only ever make
/// blocking unary, client-streaming, and server-streaming calls -- this is a security
/// concern, not a feature, so it shouldn't have a gap waiting for whoever adds a duplex
/// call later to remember to plug.
/// </summary>
public class ApiKeyClientInterceptor(string? apiKey) : Interceptor
{
    public const string MetadataKey = "x-api-key";

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, Attach(context));
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, Attach(context));
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(Attach(context));
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(request, Attach(context));
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return continuation(Attach(context));
    }

    private ClientInterceptorContext<TRequest, TResponse> Attach<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        if (string.IsNullOrEmpty(apiKey))
            return context;

        var headers = context.Options.Headers ?? new Metadata();
        headers.Add(MetadataKey, apiKey);

        var optionsWithHeaders = context.Options.WithHeaders(headers);
        return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, optionsWithHeaders);
    }
}
