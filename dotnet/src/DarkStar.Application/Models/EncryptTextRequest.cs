namespace DarkStar.Application.Models;

public sealed record EncryptTextRequest(
    string Plaintext,
    string Passphrase,
    string Algorithm
);
