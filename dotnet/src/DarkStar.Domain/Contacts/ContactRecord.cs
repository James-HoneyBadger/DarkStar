namespace DarkStar.Domain.Contacts;

public sealed record ContactRecord(
    string Name,
    string? Email,
    string? Notes,
    DateTimeOffset CreatedAt
);
