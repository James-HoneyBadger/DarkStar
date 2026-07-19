namespace DarkStar.Application.Models;

public sealed record VerifyBackupResult(
    bool IsValid,
    string IntegrityHash,
    bool IsSignaturePresent,
    bool IsSignatureValid,
    string? ManifestSignatureAlgorithm,
    int KeyCount,
    int ContactCount,
    int AuditCount
);
