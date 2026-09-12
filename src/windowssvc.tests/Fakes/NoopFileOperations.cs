using ArcusWinSvc.Interfaces;

// FileOperations (the enum) lives directly in the ArcusWinSvc namespace, not .Interfaces
using ArcusWinSvc;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// Stands in for LocalFileOperations in tests that construct LocalDataAccess but never
/// call RemoveRequest, so real file deletion never needs to be exercised.
/// </summary>
public class NoopFileOperations : IFileOperations
{
    public void Delete(string path, FileOperations fileOperations)
    {
    }
}
