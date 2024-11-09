using Microsoft.Extensions.Logging;

namespace ArcusCli;

public class GetRunner(ILogger<GetRunner> logger, string[] args) : 
    AbstractBaseRunner<GetRunner>(logger, args)
{
    public override CliCommand Command { get; } = CliCommand.Get;

    public override void Run()
    {
        throw new NotImplementedException();
    }
}