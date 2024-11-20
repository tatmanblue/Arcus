namespace ArcusCli;

/// <summary>
/// all of the default commands available
/// the commands themselves should be pretty clear
/// input required for each command is specific to the command and
/// is handled in the IArgumentRunner implementation
/// </summary>
public enum CliCommand
{
    Add,
    Get,
    Remove,
    Update,
    List,
    Erase,
    Config,
    Help
}

public enum ConvertTypes
{
    None,
    MP3
}