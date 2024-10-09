using System;
using ArcusCli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
    })
    .AddTransient<CliConfiguration>()
    .BuildServiceProvider();

ILogger logger = serviceProvider.GetService<ILogger<Program>>();

logger.LogInformation("Welcome to Arcus CLI");

var cli = CliConfiguration.GetConfiguration(args, serviceProvider);

return 0;