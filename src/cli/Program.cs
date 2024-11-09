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
int ret = 1;

try
{
    
    logger.LogDebug($"RECEIVED arguments: {string.Join(" ", args)}");
    
    CliConfiguration.GetConfiguration(args, serviceProvider).Runner.Run();
    logger.LogInformation("Good bye");
    ret = 0;
}
catch (CliInvalidInputException iEx)
{
    logger.LogWarning(iEx.Message);
}
catch (CliArgumentException)
{
    // force the help output
    logger.LogInformation(HelpRunner.GetHelp());
}
catch (Exception ex)
{
    logger.LogError(ex, $"Arcus CLI failed. {ex.GetType().Name}"); 
}

return ret;