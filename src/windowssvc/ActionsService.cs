﻿using Grpc.Core;          // For gRPC core components like Server, ServerPort
using Google.Protobuf.WellKnownTypes;
using Arcus.GRPC;


namespace ArcusWinSvc;

/// <summary>
///  GRPC interface for receiving commands from other processes like the ArcusCLI
/// </summary>
public class ActionsServiceImpl : ActionsService.ActionsServiceBase
{
    private ILogger<ActionsServiceImpl> logger;
    private IndexFileManager indexManager;
    private LocalDataStore localDataStore;

    public ActionsServiceImpl(ILogger<ActionsServiceImpl> logger, 
        IndexFileManager indexManager, LocalDataStore localDataStore)
    {
        this.logger = logger;
        this.indexManager = indexManager;
        this.localDataStore = localDataStore;
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

        if (null != record && localDataStore.RemoveRequest(record))
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
        LocalDataStoreStream ldss = null;

        // TODO this replaces localDataStore.AddRequest(addRecord);
        // TODO refactor to follow pattern
        
        try
        {
            // Read the incoming file stream from the client
            while (await request.MoveNext())
            {
                var currentRequest = request.Current;

                if (null == addRecord)
                {
                    addRecord = new IndexFileRecord()
                    {
                        ShortName = request.Current.ShortName,
                        OriginFullPath = "NA",
                        Status = FileStatuses.PENDING
                    };

                    ldss = localDataStore.GetFileStream(addRecord);
                }

                // Write the current chunk to the file
                await ldss.WriteBytes(currentRequest.ChunkData.ToByteArray());
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
            ldss?.Close();
        }
    }
    
    public override async Task Get(
        GetRequest request,
        IServerStreamWriter<GetResponse> responseStream,
        ServerCallContext context)
    {
        IndexFileRecord record = indexManager.GetRecord(request.Id);
        
        // TODO how should this code below be refactored to mirror pattern as 
        // TODO this replaces localDataStore.GetRequest(record, request.DestinationPath);
        using LocalDataStoreStream ldss = localDataStore.GetFileStream(record);

        var buffer = new byte[8192];                // 8KB buffer size, TODO get from config
        int bytesRead;

        while ((bytesRead = await ldss.ReadBytes(buffer, buffer.Length)) > 0)
        {
            var response = new GetResponse()
            {
                ChunkData = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
            };

            await responseStream.WriteAsync(response);
        }
    }
}