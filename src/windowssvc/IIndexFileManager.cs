namespace ArcusWinSvc;

/// <summary>
/// the Index tells the system how to identify a file in the system
/// the manager determines where it is located and knows how to translate
/// IndexFileRecord to the index format.
/// </summary>
public interface IIndexFileManager
{
    List<IndexFileRecord> GetAllRecords();

    IndexFileRecord GetRecord(string id);

    void AddRecord(IndexFileRecord record);

    void RemoveRecord(IndexFileRecord record);
}