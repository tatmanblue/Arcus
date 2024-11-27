using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Security.Runners;

public class LocalFileDeleteRunner(string path, FileOperations fileOperations) : IRunnable
{
    public void Run()
    {
        throw new NotImplementedException();
    }
}