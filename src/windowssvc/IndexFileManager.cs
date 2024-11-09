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
    
    private static readonly object _lock = new object();
    
    public IndexFileManager(ILogger<IndexFileManager> logger)
    {
        this.logger = logger;
        string indexFile = GetIndexFile();
        if (!File.Exists(indexFile))
        {
            string fullPath = GetIndexLocation();
            logger.LogInformation($"Creating directory: {fullPath}");
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
            return;
        }
        string fileData = File.ReadAllText(indexFile);
        records = JsonConvert.DeserializeObject<ConcurrentBag<IndexFileRecord>>(fileData);
        this.logger.LogInformation($"Loaded {records.Count} records");
    }

    private string GetIndexLocation()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(path, @"Arcus FS");
    }
    
    private string GetIndexFile()
    {

        return $"{GetIndexLocation()}\\index.txt";
    }

    public List<IndexFileRecord> GetAllRecords()
    {
        // make a copy so that other processes do not change consumer use
        return records.ToList();
    }

    public void AddRecord(IndexFileRecord record)
    {
        lock (_lock)
        {
            // TODO will need to deal with concurency
            records.Add(record);
            string indexFile = GetIndexFile();
            string json = JsonConvert.SerializeObject(records);
            File.WriteAllText(indexFile, json);
        }
    }
}