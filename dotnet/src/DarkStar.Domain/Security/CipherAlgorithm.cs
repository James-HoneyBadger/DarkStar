namespace DarkStar.Domain.Security;

public enum CipherAlgorithm
{
    Aes256Gcm,
    ChaCha20Poly1305
}

public static class CipherAlgorithmParser
{
    public static CipherAlgorithm Parse(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant().Replace("-", string.Empty) ?? "aes256gcm";

        return normalized switch
        {
            "aes" => CipherAlgorithm.Aes256Gcm,
            "aes256gcm" => CipherAlgorithm.Aes256Gcm,
            "chacha" => CipherAlgorithm.ChaCha20Poly1305,
            "chacha20poly1305" => CipherAlgorithm.ChaCha20Poly1305,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported cipher algorithm")
        };
    }
}
