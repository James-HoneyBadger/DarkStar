namespace DarkStar.Application.Models;

public sealed record VerifyBackupRequest(
    string BackupPath,
    string Passphrase,
    string Algorithm,
    string? SigningSecret = null,
    string? SigningPublicKeyPem = null,
    string? SignatureMode = null
);
