namespace DarkStar.Api.Contracts;

public sealed record DecryptTextApiRequest(
    string CiphertextBase64,
    string Passphrase,
    string Algorithm = "aes256gcm"
);
