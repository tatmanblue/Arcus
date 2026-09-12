using ArcusWinSvc.Interfaces;

namespace ArcusWinSvc.Tests.Fakes;

public class FakeConfiguration : IConfiguration
{
    public required string StoreLocation { get; init; }

    public string IndexFile => Path.Combine(StoreLocation, "index.txt");

    public string IndexFilePath => StoreLocation;

    public int GrpcPort { get; init; } = 5001;

    public int GrpcMaxMessageSize { get; init; } = 10 * 1024;
}
