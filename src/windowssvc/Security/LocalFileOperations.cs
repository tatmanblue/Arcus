using ArcusWinSvc.Interfaces;
using ArcusWinSvc.Security.Runners;

namespace ArcusWinSvc.Security;

/// <summary>
/// All file operations (move, copy, delete) when operating on the local file system
/// Acts as a factory for creating the runner that will be placed into the queue and executed
/// </summary>
/// <param name="logger"></param>
/// <param name="workRunner"></param>
public class LocalFileOperations(ILogger<LocalFileOperations> logger, IWorkQueue workRunner) : IFileOperations
{
    public void Delete(string path, FileOperations fileOperations)
    {
        var deleteFile = new LocalFileDeleteRunner(path, fileOperations);
        workRunner.AddRunner(deleteFile);
        logger.LogInformation($"LFO Deleted {path}");
    }
}