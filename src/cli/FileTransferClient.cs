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
    public async Task UploadFileAsync(string shortName, string filePath)
    {
        // Create the gRPC call for streaming the file
        using var call = client.UploadFile();

        var buffer = new byte[8192];            // 8KB chunk size, TODO get from config
        int bytesRead = 0;

        try
        {
            // Open the file stream for reading
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                // Create the upload request with the chunk data
                var request = new UploadFileRequest
                {
                    ShortName = shortName,
                    ChunkData = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
                };

                // Send the chunk to the server
                await call.RequestStream.WriteAsync(request);
            }

            // Mark the stream as complete
            await call.RequestStream.CompleteAsync();

            // Get the response from the server
            var response = await call.ResponseAsync;

            Console.WriteLine($"File upload success: {response.Success}, Message: {response.Message}");
        }
        catch (RpcException rpcEx)
        {
            Console.WriteLine($"gRPC error: {rpcEx.Status.Detail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
        }
    }    
}