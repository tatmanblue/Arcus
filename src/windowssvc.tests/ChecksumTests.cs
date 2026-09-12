using System.Security.Cryptography;
using System.Text;
using Arcus.GRPC;
using ArcusWinSvc.Tests.Fakes;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArcusWinSvc.Tests;

public class ChecksumTests
{
    // ActionsServiceImpl's methods never touch the ServerCallContext parameter, so tests
    // pass null for it rather than pulling in a gRPC testing package just to construct one.
    private static readonly ServerCallContext NoContext = null!;

    private static byte[] Sha256Of(byte[] content) => SHA256.HashData(content);

    private static string HexOf(byte[] hash) => Convert.ToHexString(hash).ToLowerInvariant();

    [Fact]
    public async Task Add_ComputesChecksum_MatchingUploadedContent()
    {
        var fileAccess = new InMemoryFileAccess();
        var indexManager = new FakeIndexFileManager();
        var sut = new ActionsServiceImpl(NullLogger<ActionsServiceImpl>.Instance, indexManager, fileAccess);

        byte[] content = Encoding.UTF8.GetBytes("hello arcus, this is test content");
        var reader = new FakeAsyncStreamReader<AddRequest>(new[]
        {
            new AddRequest { ShortName = "test.txt", OriginFullPath = "test.txt", ChunkData = ByteString.CopyFrom(content) }
        });

        AddResponse response = await sut.Add(reader, NoContext);

        IndexFileRecord? record = indexManager.GetRecord(response.Id);
        Assert.NotNull(record);
        Assert.Equal(HexOf(Sha256Of(content)), record!.Checksum);
        Assert.Equal("none", record.CipherVersion);
    }

    [Fact]
    public async Task Add_ComputesChecksum_AcrossMultipleChunks()
    {
        var fileAccess = new InMemoryFileAccess();
        var indexManager = new FakeIndexFileManager();
        var sut = new ActionsServiceImpl(NullLogger<ActionsServiceImpl>.Instance, indexManager, fileAccess);

        byte[] part1 = Encoding.UTF8.GetBytes("first chunk of the file, ");
        byte[] part2 = Encoding.UTF8.GetBytes("second chunk of the file");
        byte[] whole = part1.Concat(part2).ToArray();

        var reader = new FakeAsyncStreamReader<AddRequest>(new[]
        {
            new AddRequest { ShortName = "multi.txt", OriginFullPath = "multi.txt", ChunkData = ByteString.CopyFrom(part1) },
            new AddRequest { ChunkData = ByteString.CopyFrom(part2) }
        });

        AddResponse response = await sut.Add(reader, NoContext);

        IndexFileRecord? record = indexManager.GetRecord(response.Id);
        Assert.Equal(HexOf(Sha256Of(whole)), record!.Checksum);
    }

    [Fact]
    public async Task Get_StreamsOriginalContent_WhenChecksumMatches()
    {
        var fileAccess = new InMemoryFileAccess();
        var indexManager = new FakeIndexFileManager();
        var sut = new ActionsServiceImpl(NullLogger<ActionsServiceImpl>.Instance, indexManager, fileAccess);

        byte[] content = Encoding.UTF8.GetBytes("round trip me");
        var addReader = new FakeAsyncStreamReader<AddRequest>(new[]
        {
            new AddRequest { ShortName = "roundtrip.txt", OriginFullPath = "roundtrip.txt", ChunkData = ByteString.CopyFrom(content) }
        });
        AddResponse added = await sut.Add(addReader, NoContext);

        var writer = new FakeServerStreamWriter<GetResponse>();
        await sut.Get(new GetRequest { Id = added.Id }, writer, NoContext);

        byte[] received = writer.Written.SelectMany(r => r.ChunkData.ToByteArray()).ToArray();
        Assert.Equal(content, received);
    }

    [Fact]
    public async Task Get_Throws_WhenStoredChecksumDoesNotMatchStoredBytes()
    {
        var fileAccess = new InMemoryFileAccess();
        var indexManager = new FakeIndexFileManager();
        var sut = new ActionsServiceImpl(NullLogger<ActionsServiceImpl>.Instance, indexManager, fileAccess);

        byte[] content = Encoding.UTF8.GetBytes("this content will be tampered with");
        var addReader = new FakeAsyncStreamReader<AddRequest>(new[]
        {
            new AddRequest { ShortName = "tampered.txt", OriginFullPath = "tampered.txt", ChunkData = ByteString.CopyFrom(content) }
        });
        AddResponse added = await sut.Add(addReader, NoContext);

        // Simulate corruption/tampering: the stored bytes no longer match the recorded checksum.
        IndexFileRecord record = indexManager.GetRecord(added.Id)!;
        record.Checksum = HexOf(Sha256Of(Encoding.UTF8.GetBytes("different content entirely")));

        var writer = new FakeServerStreamWriter<GetResponse>();
        RpcException ex = await Assert.ThrowsAsync<RpcException>(
            () => sut.Get(new GetRequest { Id = added.Id }, writer, NoContext));

        Assert.Equal(StatusCode.DataLoss, ex.StatusCode);
    }

    [Fact]
    public async Task Get_Succeeds_WhenRecordPredatesChecksums()
    {
        var fileAccess = new InMemoryFileAccess();
        var indexManager = new FakeIndexFileManager();
        var sut = new ActionsServiceImpl(NullLogger<ActionsServiceImpl>.Instance, indexManager, fileAccess);

        // Seed a record the way one already on disk before this change would look:
        // content present, Checksum left at its default (empty).
        byte[] content = Encoding.UTF8.GetBytes("legacy file, no checksum on record");
        var legacyRecord = new IndexFileRecord { ShortName = "legacy.txt" };
        indexManager.AddRecord(legacyRecord);
        fileAccess.Seed(legacyRecord.Id, content);

        var writer = new FakeServerStreamWriter<GetResponse>();
        await sut.Get(new GetRequest { Id = legacyRecord.Id }, writer, NoContext);

        byte[] received = writer.Written.SelectMany(r => r.ChunkData.ToByteArray()).ToArray();
        Assert.Equal(content, received);
    }
}
