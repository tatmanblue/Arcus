using System.Net;
using System.Security.Authentication;
using ArcusWinSvc;
using ArcusWinSvc.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
var config = new ArcusWinSvc.Configuration();


builder.Services.AddSingleton<ArcusWinSvc.Interfaces.IConfiguration>(config);
builder.Services.AddSingleton<IFileAccess, LocalDataAccess>();
builder.Services.AddSingleton<IIndexFileManager, IndexFileManager>();

builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>();
builder.Services.AddGrpc().AddServiceOptions<ActionsServiceImpl>(options =>
{
    options.MaxReceiveMessageSize = config.GrpcMaxMessageSize;
});
builder.WebHost.ConfigureKestrel(kestrelOptions =>
{
    // Setup a HTTP/2 endpoint without TLS.  This is for listening for GRPC calls
    kestrelOptions.Listen(IPAddress.Any, config.GrpcPort, o =>
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

Console.Out.Flush();