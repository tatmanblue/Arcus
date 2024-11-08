using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace ArcusWinSvc;

/// <summary>
/// When the server acts on a file, meta data is stored in the index file
/// and serves as the entry point for file functions
///
/// TODO may want to create an interface this or consume interfaces for reading/write,
/// TODO especially if I move this to the cloud and will not be using harddrive FS  
/// </summary>
public class IndexFileManager
{
    private ConcurrentBag<IndexFileRecord> records = new ();
    private ILogger<IndexFileManager> logger;
    
    public IndexFileManager(ILogger<IndexFileManager> logger)
    {
        this.logger = logger;
        string indexFile = $"{GetIndexFilePath()}\\index.index";
        if (!File.Exists(indexFile)) return;
        string fileData = File.ReadAllText(indexFile);
        records = JsonConvert.DeserializeObject<ConcurrentBag<IndexFileRecord>>(fileData);
        this.logger.LogInformation($"Loaded {records.Count} records");
    }

    private string GetIndexFilePath()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(path, @"Arcus FS");
    }

    public List<IndexFileRecord> GetAllRecords()
    {
        // make a copy so that other processes do not change consumer use
        return records.ToList();
    }
}