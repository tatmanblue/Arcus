namespace ArcusWinSvc.Interfaces;

public interface IFileOperations
{
    /// <summary>
    /// Can be used to delete a file anywhere on the system! so be careful
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileOperations"></param>
    void Delete(string path, FileOperations fileOperations);
}