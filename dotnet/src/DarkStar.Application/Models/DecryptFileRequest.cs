namespace DarkStar.Application.Models;

public sealed record DecryptFileRequest(
    string InputPath,
    string? OutputPath,
    string Passphrase,
    string Algorithm
);
