using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace ArcusCli.Tests;

public class PinnedThumbprintValidatorTests
{
    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=arcus-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public void Matches_ReturnsTrue_WhenThumbprintsMatchCaseInsensitively()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate();

        Assert.True(PinnedThumbprintValidator.Matches(certificate, certificate.Thumbprint.ToLowerInvariant()));
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenThumbprintsDiffer()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate();

        Assert.False(PinnedThumbprintValidator.Matches(certificate, new string('0', 40)));
    }

    [Fact]
    public void Matches_ReturnsFalse_WhenCertificateIsNull()
    {
        Assert.False(PinnedThumbprintValidator.Matches(null, "anything"));
    }
}
