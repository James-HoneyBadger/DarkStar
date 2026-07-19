namespace DarkStar.Api.Contracts;

public sealed record CreateBackupRequestDto(
    string OutputPath,
    string Passphrase,
    string Algorithm = "Aes256Gcm",
    string? SigningSecret = null,
    string? SigningPrivateKeyPem = null,
    string? SignatureMode = null
);

public sealed record CreateBackupResponseDto(
    string BackupPath,
    string IntegrityHash,
    string? ManifestSignature,
    string? ManifestSignatureAlgorithm,
    long ByteCount,
    DateTimeOffset CreatedAt
);

public sealed record VerifyBackupRequestDto(
    string BackupPath,
    string Passphrase,
    string Algorithm = "Aes256Gcm",
    string? SigningSecret = null,
    string? SigningPublicKeyPem = null,
    string? SignatureMode = null
);

public sealed record VerifyBackupResponseDto(
    bool IsValid,
    string IntegrityHash,
    bool IsSignaturePresent,
    bool IsSignatureValid,
    string? ManifestSignatureAlgorithm,
    int KeyCount,
    int ContactCount,
    int AuditCount
);

public sealed record RestoreBackupRequestDto(
    string BackupPath,
    string Passphrase,
    string Algorithm = "Aes256Gcm",
    string? SigningSecret = null,
    string? SigningPublicKeyPem = null,
    string? SignatureMode = null
);

public sealed record RestoreBackupResponseDto(
    int KeyCount,
    int ContactCount,
    int AuditCount,
    string IntegrityHash,
    bool SignatureVerified,
    string? ManifestSignatureAlgorithm
);
