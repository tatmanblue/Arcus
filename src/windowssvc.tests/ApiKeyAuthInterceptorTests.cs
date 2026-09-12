using ArcusWinSvc.Security;
using ArcusWinSvc.Tests.Fakes;
using Grpc.Core;
using Xunit;
using IConfiguration = ArcusWinSvc.Interfaces.IConfiguration;

namespace ArcusWinSvc.Tests;

public class ApiKeyAuthInterceptorTests
{
    private class FixedConfiguration(IReadOnlyCollection<string> apiKeys) : IConfiguration
    {
        public string IndexFile => string.Empty;
        public string IndexFilePath => string.Empty;
        public string StoreLocation => string.Empty;
        public int GrpcPort { get; init; } = 5001;
        public int GrpcMaxMessageSize { get; init; } = 10 * 1024;
        public string? TlsCertificatePath => null;
        public string? TlsCertificatePassword => null;
        public IReadOnlyCollection<string> ApiKeys => apiKeys;
    }

    private static Task<string> Echo(string request, ServerCallContext context) => Task.FromResult(request);

    [Fact]
    public async Task UnaryServerHandler_AllowsCall_WhenNoKeysConfigured()
    {
        var interceptor = new ApiKeyAuthInterceptor(new FixedConfiguration(Array.Empty<string>()));
        var context = new FakeServerCallContext();

        string result = await interceptor.UnaryServerHandler("hello", context, Echo);

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task UnaryServerHandler_AllowsCall_WhenKeyMatches()
    {
        var interceptor = new ApiKeyAuthInterceptor(new FixedConfiguration(new[] { "correct-key" }));
        var headers = new Metadata { { ApiKeyAuthInterceptor.MetadataKey, "correct-key" } };
        var context = new FakeServerCallContext(headers);

        string result = await interceptor.UnaryServerHandler("hello", context, Echo);

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task UnaryServerHandler_Throws_WhenKeyIsMissing()
    {
        var interceptor = new ApiKeyAuthInterceptor(new FixedConfiguration(new[] { "correct-key" }));
        var context = new FakeServerCallContext();

        RpcException ex = await Assert.ThrowsAsync<RpcException>(
            () => interceptor.UnaryServerHandler("hello", context, Echo));

        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task UnaryServerHandler_Throws_WhenKeyDoesNotMatch()
    {
        var interceptor = new ApiKeyAuthInterceptor(new FixedConfiguration(new[] { "correct-key" }));
        var headers = new Metadata { { ApiKeyAuthInterceptor.MetadataKey, "wrong-key" } };
        var context = new FakeServerCallContext(headers);

        await Assert.ThrowsAsync<RpcException>(() => interceptor.UnaryServerHandler("hello", context, Echo));
    }

    [Fact]
    public async Task UnaryServerHandler_AllowsCall_WhenOneOfMultipleKeysMatches()
    {
        var interceptor = new ApiKeyAuthInterceptor(new FixedConfiguration(new[] { "key-a", "key-b", "key-c" }));
        var headers = new Metadata { { ApiKeyAuthInterceptor.MetadataKey, "key-b" } };
        var context = new FakeServerCallContext(headers);

        string result = await interceptor.UnaryServerHandler("hello", context, Echo);

        Assert.Equal("hello", result);
    }
}
