using System.Security.Cryptography;
using System.Text;
using DarkStar.Application.Abstractions;
using DarkStar.Domain.Security;

namespace DarkStar.Infrastructure.Crypto;

public sealed class AesGcmEncryptionEngine : IEncryptionEngine
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public byte[] EncryptBytes(ReadOnlySpan<byte> plaintext, string passphrase, CipherAlgorithm algorithm)
    {
        // For the first migration slice, ChaCha20 requests are mapped to AES-GCM
        // until a dedicated implementation lands.
        _ = algorithm;

        var key = SHA256.HashData(Encoding.UTF8.GetBytes(passphrase));

        Span<byte> nonce = stackalloc byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload.AsSpan(0, NonceSize));
        tag.CopyTo(payload.AsSpan(NonceSize, TagSize));
        ciphertext.CopyTo(payload.AsSpan(NonceSize + TagSize));

        return payload;
    }

    public byte[] DecryptBytes(ReadOnlySpan<byte> ciphertextPayload, string passphrase, CipherAlgorithm algorithm)
    {
        _ = algorithm;

        if (ciphertextPayload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext payload is invalid");
        }

        var nonce = ciphertextPayload[..NonceSize];
        var tag = ciphertextPayload.Slice(NonceSize, TagSize);
        var ciphertext = ciphertextPayload[(NonceSize + TagSize)..];

        var plaintext = new byte[ciphertext.Length];
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(passphrase));

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public string EncryptToBase64(string plaintext, string passphrase, CipherAlgorithm algorithm)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var payload = EncryptBytes(plaintextBytes, passphrase, algorithm);
        return Convert.ToBase64String(payload);
    }

    public string DecryptFromBase64(string ciphertextBase64, string passphrase, CipherAlgorithm algorithm)
    {
        var payload = Convert.FromBase64String(ciphertextBase64);
        var plaintext = DecryptBytes(payload, passphrase, algorithm);

        return Encoding.UTF8.GetString(plaintext);
    }
}
