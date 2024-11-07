using ArcusWinSvc;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>();
builder.Services.AddGrpc().AddServiceOptions<ActionsServiceImpl>(options =>
{
    options.MaxReceiveMessageSize = 10 * 1024;
});

var host = builder.Build();
host.MapGrpcService<ActionsServiceImpl>();

// Run the host
host.Run();
