namespace DarkStar.Application.Models;

public sealed record DecryptTextResult(
    string Algorithm,
    string Plaintext
);
