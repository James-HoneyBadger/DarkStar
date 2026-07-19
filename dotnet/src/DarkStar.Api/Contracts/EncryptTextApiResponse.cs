namespace DarkStar.Api.Contracts;

public sealed record EncryptTextApiResponse(
    string Algorithm,
    string CiphertextBase64
);
