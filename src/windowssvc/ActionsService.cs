using Grpc.Core;          // For gRPC core components like Server, ServerPort
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

    public ActionsServiceImpl(ILogger<ActionsServiceImpl> logger, IndexFileManager indexManager)
    {
        this.logger = logger;
        this.indexManager = indexManager;
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
    
    public override Task<AddResponse> Add(AddRequest request, ServerCallContext context)
    {
        // TODO need to validation, shadow data etc
        // TODO need to actually copy the file to storage

        IndexFileRecord addRecord = new IndexFileRecord()
        {
            ShortName = request.ShortName,
            OriginFullPath = request.OriginFullPath,
            Status = FileStatuses.VALID
        };
        
        indexManager.AddRecord(addRecord);
        
        var response = new AddResponse()
        {
            Status = (Arcus.GRPC.FileStatuses) addRecord.Status,
            Id = addRecord.Id
        };
        
        return Task.FromResult(response);
    }

    public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
    {
        var response = new GetResponse()
        {
            Success = false
        };
        
        IndexFileRecord record = indexManager.GetRecord(request.Id);

        response.Success = true;
        
        return Task.FromResult(response);
    }
}