using Grpc.Core;

namespace ArcusWinSvc.Tests.Fakes;

/// <summary>
/// Replays a fixed sequence of messages as an IAsyncStreamReader, as if a gRPC client
/// had streamed them.
/// </summary>
public class FakeAsyncStreamReader<T> : IAsyncStreamReader<T> where T : class
{
    private readonly Queue<T> items;

    public FakeAsyncStreamReader(IEnumerable<T> items)
    {
        this.items = new Queue<T>(items);
    }

    public T Current { get; private set; } = null!;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return Task.FromResult(false);

        Current = items.Dequeue();
        return Task.FromResult(true);
    }
}
