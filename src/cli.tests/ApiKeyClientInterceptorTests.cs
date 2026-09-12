using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Xunit;

namespace ArcusCli.Tests;

public class ApiKeyClientInterceptorTests
{
    private static readonly Method<string, string> TestMethod = new(
        MethodType.Unary,
        "TestService",
        "TestMethod",
        Marshallers.Create<string>(Encoding.UTF8.GetBytes, Encoding.UTF8.GetString),
        Marshallers.Create<string>(Encoding.UTF8.GetBytes, Encoding.UTF8.GetString));

    private static ClientInterceptorContext<string, string> CreateContext(Metadata? headers = null)
    {
        CallOptions options = headers is null ? new CallOptions() : new CallOptions().WithHeaders(headers);
        return new ClientInterceptorContext<string, string>(TestMethod, "localhost", options);
    }

    [Fact]
    public void BlockingUnaryCall_AddsApiKeyHeader_WhenKeyIsConfigured()
    {
        var interceptor = new ApiKeyClientInterceptor("my-api-key");
        Metadata? capturedHeaders = null;

        interceptor.BlockingUnaryCall("request", CreateContext(), (req, ctx) =>
        {
            capturedHeaders = ctx.Options.Headers;
            return "response";
        });

        Metadata.Entry? entry = capturedHeaders?.Get(ApiKeyClientInterceptor.MetadataKey);
        Assert.NotNull(entry);
        Assert.Equal("my-api-key", entry!.Value);
    }

    [Fact]
    public void BlockingUnaryCall_DoesNotAddHeader_WhenNoKeyIsConfigured()
    {
        var interceptor = new ApiKeyClientInterceptor(null);
        Metadata? capturedHeaders = null;

        interceptor.BlockingUnaryCall("request", CreateContext(), (req, ctx) =>
        {
            capturedHeaders = ctx.Options.Headers;
            return "response";
        });

        Assert.Null(capturedHeaders?.Get(ApiKeyClientInterceptor.MetadataKey));
    }

    [Fact]
    public void BlockingUnaryCall_PreservesExistingHeaders_WhenAddingApiKey()
    {
        var existing = new Metadata { { "existing-header", "existing-value" } };
        var interceptor = new ApiKeyClientInterceptor("my-api-key");
        Metadata? capturedHeaders = null;

        interceptor.BlockingUnaryCall("request", CreateContext(existing), (req, ctx) =>
        {
            capturedHeaders = ctx.Options.Headers;
            return "response";
        });

        Assert.NotNull(capturedHeaders?.Get("existing-header"));
        Assert.NotNull(capturedHeaders?.Get(ApiKeyClientInterceptor.MetadataKey));
    }
}
