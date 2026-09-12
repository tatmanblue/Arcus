using ArcusWinSvc;
using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// In-memory stand-in for LocalIndexFileManager -- same contract, no disk-backed index file.
/// </summary>
public class FakeIndexFileManager : IIndexFileManager
{
    private readonly List<IndexFileRecord> records = new();

    public List<IndexFileRecord> GetAllRecords() => records.ToList();

    public IndexFileRecord GetRecord(string id) => records.FirstOrDefault(r => r.Id == id);

    public void AddRecord(IndexFileRecord record) => records.Add(record);

    public void RemoveRecord(IndexFileRecord record) => records.RemoveAll(r => r.Id == record.Id);
}
