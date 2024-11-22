using System.Net;
using System.Text.RegularExpressions;
using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Google.Protobuf.WellKnownTypes;
using Arcus.GRPC;
using ArcusWinSvc.Integrations;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;


namespace ArcusWinSvc;

/// <summary>
///  GRPC interface for receiving commands from other processes like the ArcusCLI
/// </summary>
public class ActionsServiceImpl : ActionsService.ActionsServiceBase
{
    private ILogger<ActionsServiceImpl> logger;
    private IIndexFileManager indexManager;
    private IFileAccess fileAccess;

    public ActionsServiceImpl(ILogger<ActionsServiceImpl> logger, 
        IIndexFileManager indexManager, IFileAccess fileAccess)
    {
        this.logger = logger;
        this.indexManager = indexManager;
        this.fileAccess = fileAccess;
    }
    
    public override Task<ListResponse> List(ListRequest request, ServerCallContext context)
    {
        var response = new ListResponse();
        List<IndexFileRecord> records = indexManager.GetAllRecords();
        response.Count = records.Count;
        foreach (IndexFileRecord record in records)
        {
            Timestamp timestamp = Timestamp.FromDateTime(record.Timestamp.ToUniversalTime());
            var fileRecord = new FileRecord()
            {
                Id = record.Id,
                FileName = record.ShortName,
                Date = timestamp,
                Status = (Arcus.GRPC.FileStatuses)record.Status,
            };
            fileRecord.Keywords.AddRange(record.Keywords);
            response.Files.Add(fileRecord);
        }
        
        return Task.FromResult(response);
    }

    public override Task<RemoveResponse> Remove(RemoveRequest request, ServerCallContext context)
    {
        var response = new RemoveResponse()
        {
            Success = false
        };
        
        IndexFileRecord record = indexManager.GetRecord(request.Id);

        if (null != record && fileAccess.RemoveRequest(record))
        {
            indexManager.RemoveRecord(record);
            response.Success = true;
        }
        
        return Task.FromResult(response);
    }
    
    public override async Task<AddResponse> Add(
        IAsyncStreamReader<AddRequest> request,
        ServerCallContext context)
    {
        IndexFileRecord addRecord = null;
        IFileAccessStream fas = null;
        
        try
        {
            // Read the incoming file stream from the client
            while (await request.MoveNext())
            {
                var currentRequest = request.Current;

                // this is a bit crappy that we do not get index data until
                // the first chunk of the file is also received.  We cannot initialize
                // the record, nor the stream until that first chunk is received
                if (null == addRecord)
                {
                    addRecord = new IndexFileRecord()
                    {
                        ShortName = request.Current.ShortName,
                        OriginFullPath = request.Current.OriginFullPath,
                        Keywords = request.Current.Keywords.ToList(),
                        Status = FileStatuses.PENDING
                    };

                    fas = fileAccess.AddRequest(addRecord);
                }

                // Write the current chunk to the file
                await fas.WriteBytes(currentRequest.ChunkData.ToByteArray());
            }
            
            addRecord.Status = FileStatuses.VALID;
            indexManager.AddRecord(addRecord);

            return new AddResponse()
            {
                Id = addRecord.Id,
                Status = (Arcus.GRPC.FileStatuses) addRecord.Status,
            };
        }
        finally
        {
            fas?.Close();
        }
    }
    
    public override async Task Get(
        GetRequest request,
        IServerStreamWriter<GetResponse> responseStream,
        ServerCallContext context)
    {
        IndexFileRecord record = indexManager.GetRecord(request.Id);
        
        using IFileAccessStream fas = fileAccess.GetRequest(record);

        var buffer = new byte[8192];                // 8KB buffer size, TODO get from config
        int bytesRead;

        while ((bytesRead = await fas.ReadBytes(buffer, buffer.Length)) > 0)
        {
            var response = new GetResponse()
            {
                ChunkData = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
            };

            await responseStream.WriteAsync(response);
        }
    }

    /// <summary>
    /// Currently the URL command only works on youtube videos.  There is no validation
    /// the URL is valid etc....
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<UrlResponse> Url(UrlRequest request, ServerCallContext context)
    {
        logger.LogDebug($"Url Handler got: {request.Url}");

        var response = new UrlResponse()
        {
            Status = Arcus.GRPC.FileStatuses.Error
        };

        try
        {
            (string file, string title) = await new YouTube(request.Url).ExtractAudio();
            
            string invalidCharsPattern = $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";
            title = Regex.Replace(title, invalidCharsPattern, " ");;
            
            var addRecord = new IndexFileRecord()
            {
                ShortName = Path.GetFileName(file),
                OriginFullPath = title,
                Keywords = request.Keywords.ToList(),
                Status = FileStatuses.PENDING
            };
            
            var fas = fileAccess.AddRequest(addRecord);
            await fas.LocalCopy(file);
            
            addRecord.Status = FileStatuses.VALID;
            indexManager.AddRecord(addRecord);
            
            response.Id = addRecord.Id;
            response.Status = Arcus.GRPC.FileStatuses.Valid;
        }
        catch (Exception ex)
        {
            logger.LogError($"Url Handler failed: {ex.Message}");
        }

        return await Task.FromResult(response);
    }
    
}