namespace DarkStar.Api.Contracts;

public sealed record DecryptTextApiResponse(
    string Algorithm,
    string Plaintext
);
