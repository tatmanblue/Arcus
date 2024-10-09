using System;
using ArcusCli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;


var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
        loggingBuilder.AddDebug();
    })
    .AddTransient<CliConfiguration>()
    .BuildServiceProvider();

ILogger logger = serviceProvider.GetService<ILogger<Program>>();
logger.LogInformation("Welcome to Arcus CLI");

try
{
    CliConfiguration.GetConfiguration(args, serviceProvider).Runner.Run();
    logger.LogInformation("Good bye");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Arcus CLI failed."); 
    return 1; 
}