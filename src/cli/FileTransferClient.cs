using Grpc.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.GRPC;

namespace ArcusCli;

/// <summary>
/// 
/// </summary>
public class FileTransferClient(ActionsService.ActionsServiceClient client)
{
    /// <summary>
    /// Responsible for sending the file to the service, using GRPC streams
    /// </summary>
    /// <param name="shortName"></param>
    /// <param name="filePath"></param>
    public async Task<AddResponse> UploadFileAsync(string shortName, string filePath)
    {
        using var call = client.Add();

        var buffer = new byte[8192]; // 8KB chunk size, TODO get from config
        int bytesRead = 0;

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var request = new AddRequest()
            {
                ShortName = shortName,
                ChunkData = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
            };

            await call.RequestStream.WriteAsync(request);
        }

        await call.RequestStream.CompleteAsync();
        var response = await call.ResponseAsync;

        return response;
    }

    public async Task<long> DownloadFileAsync(string id, string destination)
    {
        var request = new GetRequest() { Id = id };

        // Create the gRPC call for streaming the file download
        using var call = client.Get(request);

        // Create or overwrite the local file to save the downloaded content
        using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write);

        // Buffer size for writing chunks
        byte[] buffer;
        long bytesRead = 0;

        // Read the stream from the server in chunks
        while (await call.ResponseStream.MoveNext())
        {
            var currentChunk = call.ResponseStream.Current;

            // Convert the chunk data (ByteString) into a byte array
            buffer = currentChunk.ChunkData.ToByteArray();

            // Write the chunk to the file
            await fileStream.WriteAsync(buffer, 0, buffer.Length);
            bytesRead += buffer.Length;
        }
        
        return bytesRead;
    }
}
