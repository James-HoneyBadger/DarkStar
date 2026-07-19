namespace DarkStar.Application.Models;

public sealed record CreateKeyRequest(
    string Algorithm,
    string Label
);
