namespace DarkStar.Application.Models;

public sealed record CreateBackupRequest(
    string OutputPath,
    string Passphrase,
    string Algorithm,
    string? SigningSecret = null,
    string? SigningPrivateKeyPem = null,
    string? SignatureMode = null
);
