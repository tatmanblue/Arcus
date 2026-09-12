using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using ArcusWinSvc;
using ArcusWinSvc.Interfaces;
using ArcusWinSvc.Security;
using ArcusWinSvc.Security.Ciphers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
var config = new ArcusWinSvc.Configuration();

builder.Services.AddSingleton<ArcusWinSvc.Interfaces.IConfiguration>(config);
builder.Services.AddSingleton<IFileAccess, LocalDataAccess>();
builder.Services.AddSingleton<IIndexFileManager, LocalIndexFileManager>();
builder.Services.AddSingleton<IFileOperations, LocalFileOperations>();
builder.Services.AddSingleton<IWorkQueue, WorkQueue>();
builder.Services.AddSingleton<IKeyProvider, FileKeyProvider>();
builder.Services.AddSingleton<IStreamCipherFactory, StreamCipherFactory>();

builder.Services.AddWindowsService();
builder.Services.AddHostedService<WorkQueueRunner>();


builder.Services.AddGrpc().AddServiceOptions<ActionsServiceImpl>(options =>
{
    options.MaxReceiveMessageSize = config.GrpcMaxMessageSize;
});
builder.WebHost.ConfigureKestrel(kestrelOptions =>
{
    // TLS is enabled only when a certificate is configured (ARCUS_TLS_CERT_PATH). Left
    // unconfigured, this keeps today's cleartext HTTP/2 (h2c) behavior, so a purely local,
    // single-machine setup needs no extra configuration to keep working.
    kestrelOptions.Listen(IPAddress.Any, config.GrpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;

        if (!string.IsNullOrWhiteSpace(config.TlsCertificatePath))
        {
            var certificate = new X509Certificate2(config.TlsCertificatePath, config.TlsCertificatePassword);
            listenOptions.UseHttps(certificate);
        }
    });
    kestrelOptions.ConfigureHttpsDefaults(o =>
    {
        // SslProtocols.None lets the OS choose the most secure available protocol -- the
        // documented, recommended value, not a literal "off" switch. It only takes effect
        // on endpoints that actually call UseHttps(), i.e. only once TLS is configured above.
        o.SslProtocols = SslProtocols.None;
    });
});


var host = builder.Build();
host.MapGrpcService<ActionsServiceImpl>();
host.Run();
 
Console.Out.Flush();