using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

public class CliConfiguration
{
    private ILogger<CliConfiguration> logger;

    public string Command { get; private set; }

    public CliConfiguration(ILogger<CliConfiguration> logger)
    {
        this.logger = logger;
    }

    public static CliConfiguration GetConfiguration(string[] args, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var config = scope.ServiceProvider.GetService<CliConfiguration>();
        return config;
    }
}

