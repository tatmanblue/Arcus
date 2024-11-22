using System.Net;
using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Google.Protobuf.WellKnownTypes;
using Arcus.GRPC;
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

    public override async Task<UrlResponse> Url(UrlRequest request, ServerCallContext context)
    {
        logger.LogDebug($"Url Handler got: {request.Url}");

        var response = new UrlResponse()
        {
            Status = Arcus.GRPC.FileStatuses.Error
        };

        try
        {
            var youtube = new YoutubeClient();
            var videoUrl = request.Url;
            var video = await youtube.Videos.GetAsync(videoUrl);

            var title = video.Title;
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            var audioStreamInfo = streamManifest
                .GetAudioStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate();
            
            if (File.Exists($"f:\\downloads\\{title}.mp3"))
                File.Delete($"f:\\downloads\\{title}.mp3");
            
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, $"f:\\downloads\\{title}.mp3");

            response.Status = Arcus.GRPC.FileStatuses.Valid;
        }
        catch (Exception ex)
        {
            logger.LogDebug($"Url Handler failed: {ex.Message}");
        }

        return await Task.FromResult(response);
    }
    
}