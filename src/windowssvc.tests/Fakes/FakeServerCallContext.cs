using Grpc.Core;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// Minimal ServerCallContext exposing only the request headers a caller supplied --
/// enough to test ApiKeyAuthInterceptor without spinning up a real gRPC pipeline.
/// </summary>
public class FakeServerCallContext(Metadata? requestHeaders = null) : ServerCallContext
{
    private readonly Metadata requestHeaders = requestHeaders ?? new Metadata();

    protected override string MethodCore => "FakeMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "fake-peer";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => requestHeaders;
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => new();
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new(string.Empty, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken? CreatePropagationTokenCore(ContextPropagationOptions? options) => null;

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
