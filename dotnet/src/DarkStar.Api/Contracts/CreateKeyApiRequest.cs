namespace DarkStar.Api.Contracts;

public sealed record CreateKeyApiRequest(
    string Algorithm,
    string Label
);
