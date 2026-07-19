namespace DarkStar.Api.Contracts;

public sealed record CreateContactApiRequest(
    string Name,
    string? Email,
    string? Notes
);
