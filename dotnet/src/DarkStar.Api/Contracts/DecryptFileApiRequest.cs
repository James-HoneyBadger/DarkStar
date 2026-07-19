namespace DarkStar.Api.Contracts;

public sealed record DecryptFileApiRequest(
    string InputPath,
    string? OutputPath,
    string Passphrase,
    string Algorithm = "aes256gcm"
);
