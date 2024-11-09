namespace ArcusCli;

/// <summary>
/// Implementation for a command.
/// EG:  ArcusCli add ...
///
/// For the above example, there would be an implementation handles the add command
///
/// I did not want to call this ICommandRunner because of the potential ambiguity with
/// ICommand and ICliCommandRunner looked weird.
/// </summary>
public interface IArgumentRunner
{
    CliCommand Command { get; }
    void Run();
}