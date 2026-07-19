namespace DarkStar.Domain.Keys;

public sealed record KeyRecord(
    string Fingerprint,
    string Algorithm,
    string Label,
    DateTimeOffset CreatedAt
);
