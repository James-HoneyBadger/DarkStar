namespace DarkStar.Application.Models;

public sealed record RestoreBackupResult(
    int KeyCount,
    int ContactCount,
    int AuditCount,
    string IntegrityHash,
    bool SignatureVerified,
    string? ManifestSignatureAlgorithm
);
