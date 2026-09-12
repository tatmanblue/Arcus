using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Google.Protobuf.WellKnownTypes;
using Arcus.GRPC;
using ArcusWinSvc.Integrations;
using ArcusWinSvc.Interfaces;
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
        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

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

                // Write the current chunk to the file, hashing the same bytes so
                // integrity checking doesn't require a second read of the file later
                byte[] chunk = currentRequest.ChunkData.ToByteArray();
                hasher.AppendData(chunk);
                await fas.WriteBytes(chunk);
            }

            addRecord.Checksum = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
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
        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = new byte[8192];                // 8KB buffer size, TODO get from config
        int bytesRead;

        while ((bytesRead = await fas.ReadBytes(buffer, buffer.Length)) > 0)
        {
            hasher.AppendData(buffer, 0, bytesRead);

            var response = new GetResponse()
            {
                ChunkData = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
            };

            await responseStream.WriteAsync(response);
        }

        // A record with no checksum predates integrity checking and is not verifiable -- that's
        // fine, not a failure. Verification necessarily happens after the chunks above have already
        // been streamed to the caller (the hash isn't final until the last byte is read), so a
        // mismatch here still fails the RPC but can't stop bytes already sent.
        if (!string.IsNullOrEmpty(record.Checksum))
        {
            string actualChecksum = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
            if (!string.Equals(actualChecksum, record.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"Checksum mismatch for file {record.Id}: expected {record.Checksum}, got {actualChecksum}");
                throw new RpcException(new Status(StatusCode.DataLoss, $"Stored file failed integrity verification: {record.Id}"));
            }
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

        IFileAccessStream fas = null;

        try
        {
            (string file, string title) = await new YouTube(request.Url).ExtractAudio();

            string invalidCharsPattern = $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";
            title = Regex.Replace(title, invalidCharsPattern, " ");

            // Hash before LocalCopy, which deletes the source file once it's copied into the store
            string checksum;
            await using (var sourceStream = File.OpenRead(file))
            {
                checksum = Convert.ToHexString(await SHA256.HashDataAsync(sourceStream)).ToLowerInvariant();
            }

            var addRecord = new IndexFileRecord()
            {
                ShortName = Path.GetFileName(file),
                OriginFullPath = title,
                Keywords = request.Keywords.ToList(),
                Status = FileStatuses.PENDING,
                Checksum = checksum
            };

            fas = fileAccess.AddRequest(addRecord);
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
        finally
        {
            fas?.Close();
        }

        return await Task.FromResult(response);
    }
    
}