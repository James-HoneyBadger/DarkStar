namespace DarkStar.Application.Models;

public sealed record DecryptTextRequest(
    string CiphertextBase64,
    string Passphrase,
    string Algorithm
);
