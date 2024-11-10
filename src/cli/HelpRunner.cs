using System.Text;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// Displays the cmd line help information.
/// TODO: candidate for removal.  Instead of a runner when cmd
/// TODO: isn't found, throw exception print out help since
/// TODO: commands that have invalid inputs do that anyways
/// </summary>
public class HelpRunner : IArgumentRunner
{
    private ILogger<HelpRunner> logger;

    public HelpRunner(ILogger<HelpRunner> logger)
    {
        this.logger = logger;
    }
    
    public CliCommand Command { get; } = CliCommand.Help;
    
    public void Run()
    {       
        logger.LogInformation(HelpRunner.GetHelp());
    }
    
    public static string GetHelp()
    {
        StringBuilder builder = new();
        builder.AppendLine("Usage:");
        builder.AppendLine("  add {filename}");
        builder.AppendLine("  get --id={id} --path={path}");
        builder.AppendLine("  list");
        builder.AppendLine("  remove {id}");
        builder.AppendLine("  update {filename}");
        // builder.AppendLine("  erase {filename}");
        // builder.AppendLine("  config {json config file}");
        
        return builder.ToString();
    }
}