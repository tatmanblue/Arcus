using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Google.Protobuf.WellKnownTypes;
using Arcus.GRPC;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Grabbed;


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

        var grabber = GrabberBuilder.New()
            .UseDefaultServices()
            .AddYouTube()
            .Build();

        var response = new UrlResponse()
        {
            Status = Arcus.GRPC.FileStatuses.Error
        };

        try
        {
            GrabResult result;
            try
            {
                result = await grabber.GrabAsync(new Uri(request.Url));
            }
            catch
            {
                // see https://github.com/dotnettools/SharpGrabber/issues/97
                logger.LogDebug($"Running second attempt for URL: {request.Url}");
                result = await grabber.GrabAsync(new Uri(request.Url));
            }

            var info = result.Resource<GrabbedInfo>();
            logger.LogInformation($"URL got {info}");
            var mediaFiles = result.Resources<GrabbedMedia>().ToArray();
            var bestAudio = mediaFiles.GetHighestQualityAudio();
            logger.LogInformation($"Best audio = {bestAudio}");
            var file = await DownloadMedia(bestAudio, result);
            logger.LogInformation($"File = {file}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Url Handler failed: {ex.Message}");
        }
        
        
        return await Task.FromResult(response);
    }
    
    /// <summary>
    /// this is temporary
    /// </summary>
    /// <param name="media"></param>
    /// <param name="grabResult"></param>
    /// <returns></returns>
    private async Task<string> DownloadMedia(GrabbedMedia media, IGrabResult grabResult)
    {
        logger.LogDebug("Downloading {0}...", media.Title ?? media.FormatTitle ?? media.Resolution);
        using var response = await new HttpClient().GetAsync(media.ResourceUri);
        response.EnsureSuccessStatusCode();
        using var downloadStream = await response.Content.ReadAsStreamAsync();
        using var resourceStream = await grabResult.WrapStreamAsync(downloadStream);
        var path = "f:\\downloads\\song.mp3";

        using var fileStream = new FileStream(path, FileMode.Create);
        await resourceStream.CopyToAsync(fileStream);
        return path;
    }
}