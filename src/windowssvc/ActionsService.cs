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
                FileName = record.ShortName,
                Date = timestamp,
                Status = (Arcus.GRPC.FileStatus)record.Status,
            };
            fileRecord.Keywords.AddRange(record.Keywords);
            response.Files.Add(fileRecord);
        }
        
        return Task.FromResult(response);
    }
}