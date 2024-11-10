using System.Net;
using System.Security.Authentication;
using ArcusWinSvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<LocalDataStore>();
builder.Services.AddSingleton<IndexFileManager>();

builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>();
builder.Services.AddGrpc().AddServiceOptions<ActionsServiceImpl>(options =>
{
    options.MaxReceiveMessageSize = 10 * 1024;
});
builder.WebHost.ConfigureKestrel(kestrelOptions =>
{
    // Setup a HTTP/2 endpoint without TLS.  This is for listening for GRPC calls
    kestrelOptions.Listen(IPAddress.Any, 5001, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
    kestrelOptions.ConfigureHttpsDefaults(o =>
    {
        o.SslProtocols = SslProtocols.None;
    });
});


var host = builder.Build();
host.MapGrpcService<ActionsServiceImpl>();

// Run the host
host.Run();
