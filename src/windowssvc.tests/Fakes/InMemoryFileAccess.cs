using ArcusWinSvc;
using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// In-memory stand-in for LocalDataAccess so tests don't touch real disk.
/// Mirrors its behavior: AddRequest opens a fresh write session, GetFileStream opens
/// a fresh read session over whatever was last written for that record's id.
/// </summary>
public class InMemoryFileAccess : IFileAccess
{
    private readonly Dictionary<string, byte[]> store = new();

    public IFileAccessStream AddRequest(IndexFileRecord record)
    {
        return new InMemoryFileAccessStream(bytes => store[record.Id] = bytes);
    }

    public IFileAccessStream GetFileStream(IndexFileRecord record)
    {
        return new InMemoryFileAccessStream(store[record.Id]);
    }

    public IFileAccessStream GetRequest(IndexFileRecord record)
    {
        if (!store.ContainsKey(record.Id))
            throw new DirectoryNotFoundException();

        return GetFileStream(record);
    }

    public bool RemoveRequest(IndexFileRecord record)
    {
        return store.Remove(record.Id);
    }

    /// <summary>
    /// Test-only helper to seed content directly, bypassing Add -- used to simulate
    /// records that predate a behavior (e.g. no checksum) without going through it.
    /// </summary>
    public void Seed(string id, byte[] content)
    {
        store[id] = content;
    }
}
