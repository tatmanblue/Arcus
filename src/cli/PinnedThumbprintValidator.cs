using System.Security.Cryptography.X509Certificates;

namespace ArcusCli;

/// <summary>
/// Validates a server certificate against a single pinned thumbprint. Used when connecting
/// to a service using a self-signed certificate (see docs/PHASE_A_SECURITY_PLAN.md) --
/// instead of either failing default CA validation or disabling certificate validation
/// entirely, which would accept literally any certificate.
/// </summary>
public static class PinnedThumbprintValidator
{
    public static bool Matches(X509Certificate2? certificate, string expectedThumbprint)
    {
        return certificate is not null
            && string.Equals(certificate.Thumbprint, expectedThumbprint, StringComparison.OrdinalIgnoreCase);
    }
}
