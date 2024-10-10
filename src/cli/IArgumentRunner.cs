namespace ArcusCli;

public interface IArgumentRunner
{
    CliCommand Command { get; }
    void Run();
}