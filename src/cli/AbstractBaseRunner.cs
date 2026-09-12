using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using Arcus.GRPC;

namespace ArcusCli;

/// <summary>
/// Since most runners will be accessing the service, the base class
/// can contain some of the share behaviors
/// </summary>
public abstract class AbstractBaseRunner<T> : IDisposable, IArgumentRunner where T: IArgumentRunner
{
    private const string ARCUS_SERVICE_URL = "ARCUS_SERVICE_URL";
    private const string ARCUS_SERVICE_TLS_THUMBPRINT = "ARCUS_SERVICE_TLS_THUMBPRINT";

    /// <summary>Preserves today's behavior when ARCUS_SERVICE_URL is unset.</summary>
    public const string DefaultServiceUrl = "http://localhost:5001";

    protected ILogger<T> logger;
    protected readonly GrpcChannel channel;
    protected readonly ActionsService.ActionsServiceClient client;
    protected string[] args;

    public abstract CliCommand Command { get; }

    public AbstractBaseRunner(ILogger<T> logger, string[] args)
    {
        this.logger = logger;
        this.args = args;

        string serviceUrl = Environment.GetEnvironmentVariable(ARCUS_SERVICE_URL) ?? DefaultServiceUrl;
        string? pinnedThumbprint = Environment.GetEnvironmentVariable(ARCUS_SERVICE_TLS_THUMBPRINT);

        var channelOptions = new GrpcChannelOptions();
        if (!string.IsNullOrWhiteSpace(pinnedThumbprint))
        {
            channelOptions.HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    PinnedThumbprintValidator.Matches(certificate, pinnedThumbprint)
            };
        }

        channel = GrpcChannel.ForAddress(serviceUrl, channelOptions);
        client = new ActionsService.ActionsServiceClient(channel);
    }

    public abstract void Run();

    /// <summary>
    /// IDisposable Implementation suggestion taken from CodeRabbitAI
    /// seems like overkill but ain't going to worry about it atm
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            channel?.Dispose();
        }
    }
}
