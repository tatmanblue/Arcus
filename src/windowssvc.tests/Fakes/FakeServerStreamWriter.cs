using Grpc.Core;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// Captures everything written to it, standing in for the gRPC response stream in tests.
/// </summary>
public class FakeServerStreamWriter<T> : IServerStreamWriter<T>
{
    public List<T> Written { get; } = new();

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Written.Add(message);
        return Task.CompletedTask;
    }
}
