namespace DarkStar.Application.Models;

public sealed record EncryptTextResult(
    string Algorithm,
    string CiphertextBase64
);
