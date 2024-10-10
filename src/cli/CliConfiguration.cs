using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// The purpose of this class is to process command line arguments
/// and provide the appropriate runner for the arguements provided
/// The default behavior is help when if the arguments are not understood in any way
/// </summary>
public class CliConfiguration
{
    private ILogger<CliConfiguration> logger;
    public IArgumentRunner Runner { get; private set; } = null;

    public CliConfiguration(ILogger<CliConfiguration> logger)
    {
        this.logger = logger;
    }

    public static CliConfiguration GetConfiguration(string[] args, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var config = scope.ServiceProvider.GetService<CliConfiguration>();
     
        config.Runner = GetRunner(args, serviceProvider);
        return config;
    }

    private static IArgumentRunner GetRunner(string[] args, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        IArgumentRunner runner = new HelpRunner(serviceProvider.GetService<ILogger<HelpRunner>>());;
        
        if (args.Length == 0) return runner;
        
        switch (args[0])
        {
            case "add":
            case "get":
            case "remove":
            case "update":
            case "list":
            case "erase":
            case "config":
            case "help":
            default:
                break;
        }

        return runner;
    }
}

