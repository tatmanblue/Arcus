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
        
        string command = args[0];
        string[] cmdArgs = args.Skip(1).ToArray();
        
        ILogger<CliConfiguration> logger = serviceProvider.GetService<ILogger<CliConfiguration>>();
        logger.LogInformation($"Array Len: {args.Length} for Command: {command} - CMDARGS arguments: {string.Join(" ", cmdArgs)}");
        
        switch (command)
        {
            case "list":
                runner = new ListRunner(serviceProvider.GetService<ILogger<ListRunner>>());
                break;
            case "add":
                runner = new AddRunner(serviceProvider.GetService<ILogger<AddRunner>>(), cmdArgs);
                break;
            case "get":
                runner = new GetRunner(serviceProvider.GetService<ILogger<GetRunner>>(), cmdArgs);
                break;
            case "remove":
                runner = new RemoveRunner(serviceProvider.GetService<ILogger<RemoveRunner>>(), cmdArgs);
                break;
            case "update":
            case "erase":
            case "config":
            case "help":
            default:
                break;
        }

        return runner;
    }
}

