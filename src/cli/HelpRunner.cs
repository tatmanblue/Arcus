using System.Text;
using Microsoft.Extensions.Logging;

namespace ArcusCli;

/// <summary>
/// 
/// </summary>
public class HelpRunner : IArgumentRunner
{
    private ILogger<HelpRunner> logger;

    public HelpRunner(ILogger<HelpRunner> logger)
    {
        this.logger = logger;
    }
    
    public void Run()
    {
        StringBuilder builder = new();
        builder.AppendLine("Usage:");
        builder.AppendLine("  add {filename}");
        builder.AppendLine("  get {id}");
        builder.AppendLine("  list");
        builder.AppendLine("  remove {id}");
        builder.AppendLine("  update {filename}");
        builder.AppendLine("  erase {filename}");
        builder.AppendLine("  config {json config file}");
        
        logger.LogCritical(builder.ToString());
    }
}