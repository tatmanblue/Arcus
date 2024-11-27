using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security;

public class LocalFileOperations(ILogger<LocalFileOperations> logger, IQueueWorkRunner workRunner) : IFileOperations
{
    public void Delete(string path, FileOperations fileOperations)
    {
        throw new NotImplementedException();
    }
}