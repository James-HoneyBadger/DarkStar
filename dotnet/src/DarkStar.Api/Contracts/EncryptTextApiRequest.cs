namespace DarkStar.Api.Contracts;

public sealed record EncryptTextApiRequest(
    string Plaintext,
    string Passphrase,
    string Algorithm = "aes256gcm"
);
