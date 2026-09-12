using ArcusWinSvc.Security.Ciphers;
using Xunit;

namespace ArcusWinSvc.Tests;

public class NoneStreamCipherTests
{
    [Fact]
    public void WrapForWrite_ReturnsSameStreamInstance()
    {
        var cipher = new NoneStreamCipher();
        using var stream = new MemoryStream();

        Assert.Same(stream, cipher.WrapForWrite(stream));
    }

    [Fact]
    public void WrapForRead_ReturnsSameStreamInstance()
    {
        var cipher = new NoneStreamCipher();
        using var stream = new MemoryStream();

        Assert.Same(stream, cipher.WrapForRead(stream));
    }

    [Fact]
    public void Name_IsNone()
    {
        Assert.Equal("none", new NoneStreamCipher().Name);
    }
}
