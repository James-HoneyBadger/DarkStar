using DarkStar.Domain.Security;

namespace DarkStar.Application.Abstractions;

public interface IEncryptionEngine
{
    byte[] EncryptBytes(ReadOnlySpan<byte> plaintext, string passphrase, CipherAlgorithm algorithm);
    byte[] DecryptBytes(ReadOnlySpan<byte> ciphertextPayload, string passphrase, CipherAlgorithm algorithm);
    string EncryptToBase64(string plaintext, string passphrase, CipherAlgorithm algorithm);
    string DecryptFromBase64(string ciphertextBase64, string passphrase, CipherAlgorithm algorithm);
}
