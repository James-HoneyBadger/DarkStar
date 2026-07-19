namespace DarkStar.Application.Models;

public sealed record CreateBackupResult(
    string BackupPath,
    string IntegrityHash,
    string? ManifestSignature,
    string? ManifestSignatureAlgorithm,
    long ByteCount,
    DateTimeOffset CreatedAt
);
