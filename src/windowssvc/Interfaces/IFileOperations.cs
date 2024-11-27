namespace ArcusWinSvc.Interfaces;

/// <summary>
/// File operations are more intensive behaviors on data, such as making sure the data
/// cannot be retrieved after deleting it, or encrypting it
/// </summary>
public interface IFileOperations
{
    /// <summary>
    /// Can be used to delete a file anywhere on the system! so be careful
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileOperations"></param>
    void Delete(string path, FileOperations fileOperations);
}