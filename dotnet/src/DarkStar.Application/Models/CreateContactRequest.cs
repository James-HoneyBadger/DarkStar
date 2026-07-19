namespace DarkStar.Application.Models;

public sealed record CreateContactRequest(
    string Name,
    string? Email,
    string? Notes
);
