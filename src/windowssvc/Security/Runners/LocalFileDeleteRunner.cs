using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security.Runners;

/// <summary>
/// Runner is responsible for deleting files using the "wipe" technology in LocalFileErasure
/// </summary>
/// <param name="file"></param>
/// <param name="fileOperations"></param>
public class LocalFileDeleteRunner(string file, FileOperations fileOperations) : IRunnable
{
    public string FriendlyName { get; } = "LocalFileDelete";
    
    public void Run()
    {
        LocalFileErase erase = new();
        // for now FileOperations.ERASE is the only choice and everything else is not selectable at this time
        if (fileOperations == FileOperations.ERASE)
            erase.Settings.Options = FileErasureSettings.OverwriteOptions.ZeroData;
        else
            erase.Settings.Options = FileErasureSettings.OverwriteOptions.JustErase;
        erase.Settings.OverwriteCount = 1;
        erase.Settings.OverwriteSize = 0L;
        
        erase.Erase(file);

        string path = Path.GetDirectoryName(file);
        Directory.Delete(path, true);
    }
}