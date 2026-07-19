using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Domain.Audit;
using DarkStar.Domain.Security;

namespace DarkStar.Application.Services;

public sealed class CryptoApplicationService(
    IEncryptionEngine encryptionEngine,
    ISignatureEngine signatureEngine,
    IAuditRepository auditRepository)
{
    public async Task<EncryptTextResult> EncryptTextAsync(
        EncryptTextRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Plaintext))
        {
            throw new ArgumentException("Plaintext is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Passphrase))
        {
            throw new ArgumentException("Passphrase is required", nameof(request));
        }

        var algorithm = CipherAlgorithmParser.Parse(request.Algorithm);
        var ciphertext = encryptionEngine.EncryptToBase64(request.Plaintext, request.Passphrase, algorithm);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "encrypt_text",
                algorithm.ToString(),
                $"plaintext_bytes={request.Plaintext.Length}"
            ),
            cancellationToken
        );

        return new EncryptTextResult(algorithm.ToString(), ciphertext);
    }

    public async Task<EncryptFileResult> EncryptFileAsync(
        EncryptFileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            throw new ArgumentException("Input path is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Passphrase))
        {
            throw new ArgumentException("Passphrase is required", nameof(request));
        }

        if (!File.Exists(request.InputPath))
        {
            throw new ArgumentException("Input file does not exist", nameof(request));
        }

        var algorithm = CipherAlgorithmParser.Parse(request.Algorithm);
        var inputBytes = await File.ReadAllBytesAsync(request.InputPath, cancellationToken);
        var encrypted = encryptionEngine.EncryptBytes(inputBytes, request.Passphrase, algorithm);

        var outputPath = ResolveEncryptedPath(request.InputPath, request.OutputPath);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await File.WriteAllBytesAsync(outputPath, encrypted, cancellationToken);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "encrypt_file",
                algorithm.ToString(),
                $"input={request.InputPath};output={outputPath};bytes={inputBytes.Length}"
            ),
            cancellationToken
        );

        return new EncryptFileResult(
            algorithm.ToString(),
            request.InputPath,
            outputPath,
            inputBytes.LongLength,
            encrypted.LongLength
        );
    }

    public async Task<DecryptTextResult> DecryptTextAsync(
        DecryptTextRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CiphertextBase64))
        {
            throw new ArgumentException("Ciphertext is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Passphrase))
        {
            throw new ArgumentException("Passphrase is required", nameof(request));
        }

        var algorithm = CipherAlgorithmParser.Parse(request.Algorithm);
        var plaintext = encryptionEngine.DecryptFromBase64(request.CiphertextBase64, request.Passphrase, algorithm);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "decrypt_text",
                algorithm.ToString(),
                $"plaintext_bytes={plaintext.Length}"
            ),
            cancellationToken
        );

        return new DecryptTextResult(algorithm.ToString(), plaintext);
    }

    public async Task<DecryptFileResult> DecryptFileAsync(
        DecryptFileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InputPath))
        {
            throw new ArgumentException("Input path is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Passphrase))
        {
            throw new ArgumentException("Passphrase is required", nameof(request));
        }

        if (!File.Exists(request.InputPath))
        {
            throw new ArgumentException("Input file does not exist", nameof(request));
        }

        var algorithm = CipherAlgorithmParser.Parse(request.Algorithm);
        var inputBytes = await File.ReadAllBytesAsync(request.InputPath, cancellationToken);
        var decrypted = encryptionEngine.DecryptBytes(inputBytes, request.Passphrase, algorithm);

        var outputPath = ResolveDecryptedPath(request.InputPath, request.OutputPath);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await File.WriteAllBytesAsync(outputPath, decrypted, cancellationToken);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "decrypt_file",
                algorithm.ToString(),
                $"input={request.InputPath};output={outputPath};bytes={decrypted.Length}"
            ),
            cancellationToken
        );

        return new DecryptFileResult(
            algorithm.ToString(),
            request.InputPath,
            outputPath,
            inputBytes.LongLength,
            decrypted.LongLength
        );
    }

    public async Task<SignTextResult> SignTextAsync(
        SignTextRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Secret))
        {
            throw new ArgumentException("Secret is required", nameof(request));
        }

        var signature = signatureEngine.SignToBase64(request.Message, request.Secret);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "sign_text",
                "hmac-sha256",
                $"message_bytes={request.Message.Length}"
            ),
            cancellationToken
        );

        return new SignTextResult(signature);
    }

    public async Task<VerifyTextResult> VerifyTextAsync(
        VerifyTextRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Secret))
        {
            throw new ArgumentException("Secret is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SignatureBase64))
        {
            throw new ArgumentException("Signature is required", nameof(request));
        }

        var valid = signatureEngine.Verify(request.Message, request.Secret, request.SignatureBase64);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "verify_text",
                "hmac-sha256",
                $"valid={valid.ToString().ToLowerInvariant()}"
            ),
            cancellationToken
        );

        return new VerifyTextResult(valid);
    }

    private static string ResolveEncryptedPath(string inputPath, string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return outputPath;
        }

        return inputPath + ".dstar";
    }

    private static string ResolveDecryptedPath(string inputPath, string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return outputPath;
        }

        return inputPath.EndsWith(".dstar", StringComparison.OrdinalIgnoreCase)
            ? inputPath[..^6]
            : inputPath + ".dec";
    }
}
