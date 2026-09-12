using System.ComponentModel.DataAnnotations;

namespace ArcusWinSvc.Interfaces;

public interface IConfiguration
{
    public string IndexFile { get; }
    
    public string IndexFilePath { get; }

    public string StoreLocation { get; }
    
    [Range(1024, 65535, ErrorMessage = "gRPC port must be between 1024 and 65535")]
    public int GrpcPort { get; }

    [Range(1024, int.MaxValue, ErrorMessage = "gRPC message size must be at least 1KB")]
    public int GrpcMaxMessageSize { get; }

    /// <summary>
    /// Path to a certificate (.pfx) to enable TLS on the gRPC endpoint. Null/empty keeps
    /// today's cleartext HTTP/2 behavior -- TLS is opt-in, not required.
    /// </summary>
    public string? TlsCertificatePath { get; }

    /// <summary>Password for TlsCertificatePath, if the certificate requires one.</summary>
    public string? TlsCertificatePassword { get; }

    /// <summary>
    /// Pre-shared API keys clients must present (one per client, so any single key can be
    /// revoked later without affecting the others). Empty disables enforcement entirely --
    /// matches every other Phase A security control being opt-in.
    /// </summary>
    public IReadOnlyCollection<string> ApiKeys { get; }
}