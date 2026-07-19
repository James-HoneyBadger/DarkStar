namespace DarkStar.Application.Models;

public sealed record CreateKeyResult(
    string Fingerprint,
    string Algorithm,
    string Label,
    DateTimeOffset CreatedAt
);
