namespace DarkStar.Api.Contracts;

public sealed record EncryptFileApiRequest(
    string InputPath,
    string? OutputPath,
    string Passphrase,
    string Algorithm = "aes256gcm"
);
