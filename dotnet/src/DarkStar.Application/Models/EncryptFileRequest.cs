namespace DarkStar.Application.Models;

public sealed record EncryptFileRequest(
    string InputPath,
    string? OutputPath,
    string Passphrase,
    string Algorithm
);
