using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Domain.Audit;
using DarkStar.Domain.Contacts;
using DarkStar.Domain.Keys;
using DarkStar.Domain.Security;

namespace DarkStar.Application.Services;

public sealed class BackupApplicationService(
    IEncryptionEngine encryptionEngine,
    ISignatureEngine signatureEngine,
    IKeyRepository keyRepository,
    IContactRepository contactRepository,
    IAuditRepository auditRepository)
{
    private const int CurrentArchiveVersion = 1;
    private const int CurrentPayloadVersion = 1;
    private const string SignatureAlgHmacSha256 = "hmac-sha256";
    private const string SignatureAlgRsaPssSha256 = "rsa-pss-sha256";

    public async Task<CreateBackupResult> CreateBackupAsync(
        CreateBackupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            throw new ArgumentException("Output path is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Passphrase))
        {
            throw new ArgumentException("Passphrase is required", nameof(request));
        }

        var algorithm = CipherAlgorithmParser.Parse(request.Algorithm);

        var payload = new BackupPayload(
            Version: 1,
            CreatedAt: DateTimeOffset.UtcNow,
            Keys: await keyRepository.ListAsync(cancellationToken),
            Contacts: await contactRepository.ListAsync(cancellationToken),
            AuditRecords: await auditRepository.ReadAllAsync(cancellationToken)
        );

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var encrypted = encryptionEngine.EncryptBytes(payloadBytes, request.Passphrase, algorithm);
        var integrity = ComputeHexSha256(encrypted);

        var archive = new BackupArchive(
            Version: 1,
            Algorithm: algorithm.ToString(),
            CreatedAt: payload.CreatedAt,
            IntegrityHash: integrity,
            CiphertextBase64: Convert.ToBase64String(encrypted),
            ManifestSignature: null,
            ManifestSignatureAlgorithm: null
        );

        var signatureMode = ResolveCreateSignatureMode(request);
        var manifestMessage = BuildManifestMessage(
            archive.Version,
            archive.Algorithm,
            archive.CreatedAt,
            archive.IntegrityHash,
            archive.CiphertextBase64);

        if (string.Equals(signatureMode, SignatureAlgHmacSha256, StringComparison.Ordinal))
        {
            archive = archive with
            {
                ManifestSignature = signatureEngine.SignToBase64(manifestMessage, request.SigningSecret!),
                ManifestSignatureAlgorithm = SignatureAlgHmacSha256
            };
        }
        else if (string.Equals(signatureMode, SignatureAlgRsaPssSha256, StringComparison.Ordinal))
        {
            archive = archive with
            {
                ManifestSignature = SignWithRsaPssSha256(manifestMessage, request.SigningPrivateKeyPem!),
                ManifestSignatureAlgorithm = SignatureAlgRsaPssSha256
            };
        }

        var outDir = Path.GetDirectoryName(request.OutputPath);
        if (!string.IsNullOrWhiteSpace(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(archive);
        await File.WriteAllBytesAsync(request.OutputPath, bytes, cancellationToken);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "backup_create",
                request.OutputPath,
                $"keys={payload.Keys.Count};contacts={payload.Contacts.Count};audit={payload.AuditRecords.Count}"
            ),
            cancellationToken
        );

        return new CreateBackupResult(
            request.OutputPath,
            integrity,
            archive.ManifestSignature,
            archive.ManifestSignatureAlgorithm,
            bytes.LongLength,
            payload.CreatedAt);
    }

    public async Task<VerifyBackupResult> VerifyBackupAsync(
        VerifyBackupRequest request,
        CancellationToken cancellationToken = default)
    {
        var (payload, hash, integrityValid, signaturePresent, signatureValid, signatureAlgorithm) = await ReadAndValidateBackupAsync(
            request.BackupPath,
            request.Passphrase,
            request.Algorithm,
            request.SigningSecret,
            request.SigningPublicKeyPem,
            request.SignatureMode,
            cancellationToken
        );

        return new VerifyBackupResult(
            integrityValid && (!signaturePresent || signatureValid),
            hash,
            signaturePresent,
            signatureValid,
            signatureAlgorithm,
            payload.Keys.Count,
            payload.Contacts.Count,
            payload.AuditRecords.Count
        );
    }

    public async Task<RestoreBackupResult> RestoreBackupAsync(
        RestoreBackupRequest request,
        CancellationToken cancellationToken = default)
    {
        var (payload, hash, integrityValid, signaturePresent, signatureValid, signatureAlgorithm) = await ReadAndValidateBackupAsync(
            request.BackupPath,
            request.Passphrase,
            request.Algorithm,
            request.SigningSecret,
            request.SigningPublicKeyPem,
            request.SignatureMode,
            cancellationToken
        );

        if (!integrityValid)
        {
            throw new CryptographicException("Backup integrity verification failed");
        }

        if (signaturePresent)
        {
            if (string.Equals(signatureAlgorithm, SignatureAlgRsaPssSha256, StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(request.SigningPublicKeyPem))
                {
                    throw new CryptographicException("Backup signature is present but RSA public key was not provided");
                }
            }
            else if (string.IsNullOrWhiteSpace(request.SigningSecret))
            {
                throw new CryptographicException("Backup signature is present but signing secret was not provided");
            }

            if (!signatureValid)
            {
                throw new CryptographicException("Backup signature verification failed");
            }
        }

        await keyRepository.ReplaceAllAsync(payload.Keys, cancellationToken);
        await contactRepository.ReplaceAllAsync(payload.Contacts, cancellationToken);
        await auditRepository.ReplaceAllAsync(payload.AuditRecords, cancellationToken);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "backup_restore",
                request.BackupPath,
                $"keys={payload.Keys.Count};contacts={payload.Contacts.Count};audit={payload.AuditRecords.Count}"
            ),
            cancellationToken
        );

        return new RestoreBackupResult(
            payload.Keys.Count,
            payload.Contacts.Count,
            payload.AuditRecords.Count,
            hash,
            signaturePresent && signatureValid,
            signatureAlgorithm
        );
    }

    private async Task<(BackupPayload payload, string hash, bool integrityValid, bool signaturePresent, bool signatureValid, string? signatureAlgorithm)> ReadAndValidateBackupAsync(
        string backupPath,
        string passphrase,
        string algorithm,
        string? signingSecret,
        string? signingPublicKeyPem,
        string? requestedSignatureMode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("Backup path is required", nameof(backupPath));
        }

        if (string.IsNullOrWhiteSpace(passphrase))
        {
            throw new ArgumentException("Passphrase is required", nameof(passphrase));
        }

        if (!File.Exists(backupPath))
        {
            throw new ArgumentException("Backup file does not exist", nameof(backupPath));
        }

        var parsedAlgorithm = CipherAlgorithmParser.Parse(algorithm);
        var fileBytes = await File.ReadAllBytesAsync(backupPath, cancellationToken);
        var archive = DeserializeArchive(fileBytes, parsedAlgorithm);

        var archiveAlgorithm = CipherAlgorithmParser.Parse(archive.Algorithm);
        if (archiveAlgorithm != parsedAlgorithm)
        {
            throw new InvalidDataException("Backup algorithm does not match requested algorithm");
        }

        byte[] ciphertext;
        try
        {
            ciphertext = Convert.FromBase64String(archive.CiphertextBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Backup ciphertext is not valid base64", ex);
        }

        var computedHash = ComputeHexSha256(ciphertext);
        var hashValid = string.Equals(archive.IntegrityHash, computedHash, StringComparison.OrdinalIgnoreCase);

        var signaturePresent = !string.IsNullOrWhiteSpace(archive.ManifestSignature);
        var signatureAlgorithm = signaturePresent ? NormalizeSignatureAlgorithm(archive.ManifestSignatureAlgorithm) : null;
        var signatureValid = false;

        if (signaturePresent)
        {
            var requestedAlgorithm = string.IsNullOrWhiteSpace(requestedSignatureMode)
                ? null
                : NormalizeSignatureAlgorithm(requestedSignatureMode);
            if (!string.IsNullOrWhiteSpace(requestedAlgorithm)
                && !string.Equals(requestedAlgorithm, signatureAlgorithm, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Backup signature algorithm does not match requested signature mode");
            }

            var manifestMessage = BuildManifestMessage(
                archive.Version,
                archive.Algorithm,
                archive.CreatedAt,
                archive.IntegrityHash,
                archive.CiphertextBase64);

            if (string.Equals(signatureAlgorithm, SignatureAlgRsaPssSha256, StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(signingPublicKeyPem))
                {
                    signatureValid = VerifyWithRsaPssSha256(manifestMessage, signingPublicKeyPem, archive.ManifestSignature!);
                }
            }
            else if (!string.IsNullOrWhiteSpace(signingSecret))
            {
                signatureValid = signatureEngine.Verify(manifestMessage, signingSecret, archive.ManifestSignature!);
            }
        }

        var decrypted = encryptionEngine.DecryptBytes(ciphertext, passphrase, parsedAlgorithm);
        var payload = DeserializePayload(decrypted);

        return (payload, computedHash, hashValid, signaturePresent, signatureValid, signatureAlgorithm);
    }

    private static string? ResolveCreateSignatureMode(CreateBackupRequest request)
    {
        var explicitMode = string.IsNullOrWhiteSpace(request.SignatureMode)
            ? null
            : NormalizeSignatureAlgorithm(request.SignatureMode);

        var hasSecret = !string.IsNullOrWhiteSpace(request.SigningSecret);
        var hasPrivateKey = !string.IsNullOrWhiteSpace(request.SigningPrivateKeyPem);

        if (hasSecret && hasPrivateKey)
        {
            throw new ArgumentException("Provide either signing secret or signing private key, not both", nameof(request));
        }

        if (explicitMode is null)
        {
            if (hasSecret)
            {
                return SignatureAlgHmacSha256;
            }

            if (hasPrivateKey)
            {
                return SignatureAlgRsaPssSha256;
            }

            return null;
        }

        if (string.Equals(explicitMode, SignatureAlgHmacSha256, StringComparison.Ordinal) && !hasSecret)
        {
            throw new ArgumentException("Signing secret is required for hmac-sha256 mode", nameof(request));
        }

        if (string.Equals(explicitMode, SignatureAlgRsaPssSha256, StringComparison.Ordinal) && !hasPrivateKey)
        {
            throw new ArgumentException("Signing private key PEM is required for rsa-pss-sha256 mode", nameof(request));
        }

        return explicitMode;
    }

    private static string BuildManifestMessage(
        int version,
        string algorithm,
        DateTimeOffset createdAt,
        string integrityHash,
        string ciphertextBase64)
    {
        return string.Join("\n",
            $"v={version}",
            $"alg={algorithm}",
            $"created={createdAt:O}",
            $"hash={integrityHash}",
            $"ct={ciphertextBase64}");
    }

    private static string NormalizeSignatureAlgorithm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SignatureAlgHmacSha256;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is "hmac" or "hmac-sha256")
        {
            return SignatureAlgHmacSha256;
        }

        if (normalized is "rsa" or "rsa-pss" or "rsa-pss-sha256")
        {
            return SignatureAlgRsaPssSha256;
        }

        throw new InvalidDataException($"Unsupported backup signature algorithm '{value}'");
    }

    private static string SignWithRsaPssSha256(string message, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var payload = Encoding.UTF8.GetBytes(message);
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Convert.ToBase64String(signature);
    }

    private static bool VerifyWithRsaPssSha256(string message, string publicKeyPem, string signatureBase64)
    {
        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(signatureBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var payload = Encoding.UTF8.GetBytes(message);
        return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    private static BackupArchive DeserializeArchive(byte[] fileBytes, CipherAlgorithm fallbackAlgorithm)
    {
        using var doc = JsonDocument.Parse(fileBytes);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Backup archive root must be an object");
        }

        var version = ReadVersion(root);

        if (version == 0)
        {
            EnsureNoUnexpectedProperties(root, "CreatedAt", "IntegrityHash", "CiphertextBase64", "Algorithm", "ManifestSignature", "ManifestSignatureAlgorithm");

            var legacy = JsonSerializer.Deserialize<LegacyBackupArchive>(root.GetRawText())
                ?? throw new InvalidDataException("Legacy backup archive is invalid");
            if (string.IsNullOrWhiteSpace(legacy.IntegrityHash) || string.IsNullOrWhiteSpace(legacy.CiphertextBase64))
            {
                throw new InvalidDataException("Legacy backup archive is missing required fields");
            }

            return new BackupArchive(
                CurrentArchiveVersion,
                legacy.Algorithm ?? fallbackAlgorithm.ToString(),
                legacy.CreatedAt,
                legacy.IntegrityHash,
                legacy.CiphertextBase64,
                legacy.ManifestSignature,
                legacy.ManifestSignatureAlgorithm
            );
        }

        if (version > CurrentArchiveVersion)
        {
            throw new NotSupportedException(
                $"Backup archive version {version} is newer than supported version {CurrentArchiveVersion}");
        }

        if (version < 0)
        {
            throw new InvalidDataException("Backup archive version cannot be negative");
        }

        EnsureNoUnexpectedProperties(root, "Version", "Algorithm", "CreatedAt", "IntegrityHash", "CiphertextBase64", "ManifestSignature", "ManifestSignatureAlgorithm");

        var archive = JsonSerializer.Deserialize<BackupArchive>(root.GetRawText())
            ?? throw new InvalidDataException("Backup archive is invalid");

        if (archive.Version != CurrentArchiveVersion)
        {
            throw new InvalidDataException(
                $"Backup archive version {archive.Version} is unsupported for strict read path");
        }

        if (string.IsNullOrWhiteSpace(archive.Algorithm)
            || string.IsNullOrWhiteSpace(archive.IntegrityHash)
            || string.IsNullOrWhiteSpace(archive.CiphertextBase64))
        {
            throw new InvalidDataException("Backup archive is missing required fields");
        }

        return archive;
    }

    private static BackupPayload DeserializePayload(byte[] decrypted)
    {
        using var doc = JsonDocument.Parse(decrypted);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Backup payload root must be an object");
        }

        var version = ReadVersion(root);

        if (version == 0)
        {
            EnsureNoUnexpectedProperties(root, "CreatedAt", "Keys", "Contacts", "AuditRecords");

            var legacy = JsonSerializer.Deserialize<LegacyBackupPayload>(root.GetRawText())
                ?? throw new InvalidDataException("Legacy backup payload is invalid");

            return new BackupPayload(
                CurrentPayloadVersion,
                legacy.CreatedAt,
                legacy.Keys ?? Array.Empty<KeyRecord>(),
                legacy.Contacts ?? Array.Empty<ContactRecord>(),
                legacy.AuditRecords ?? Array.Empty<AuditRecord>()
            );
        }

        if (version > CurrentPayloadVersion)
        {
            throw new NotSupportedException(
                $"Backup payload version {version} is newer than supported version {CurrentPayloadVersion}");
        }

        if (version < 0)
        {
            throw new InvalidDataException("Backup payload version cannot be negative");
        }

        EnsureNoUnexpectedProperties(root, "Version", "CreatedAt", "Keys", "Contacts", "AuditRecords");

        var payload = JsonSerializer.Deserialize<BackupPayload>(root.GetRawText())
            ?? throw new InvalidDataException("Backup payload is invalid");

        if (payload.Version != CurrentPayloadVersion)
        {
            throw new InvalidDataException(
                $"Backup payload version {payload.Version} is unsupported for strict read path");
        }

        if (payload.Keys is null || payload.Contacts is null || payload.AuditRecords is null)
        {
            throw new InvalidDataException("Backup payload is missing required collections");
        }

        return payload;
    }

    private static int ReadVersion(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "Version", out var versionElement))
        {
            return 0;
        }

        if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version))
        {
            throw new InvalidDataException("Backup version must be an integer");
        }

        return version;
    }

    private static void EnsureNoUnexpectedProperties(JsonElement root, params string[] allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        foreach (var property in root.EnumerateObject())
        {
            if (!allowedSet.Contains(property.Name))
            {
                throw new InvalidDataException($"Unexpected backup field '{property.Name}'");
            }
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string ComputeHexSha256(ReadOnlySpan<byte> payload)
    {
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record BackupArchive(
        int Version,
        string Algorithm,
        DateTimeOffset CreatedAt,
        string IntegrityHash,
        string CiphertextBase64,
        string? ManifestSignature,
        string? ManifestSignatureAlgorithm
    );

    private sealed record BackupPayload(
        int Version,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<KeyRecord> Keys,
        IReadOnlyCollection<ContactRecord> Contacts,
        IReadOnlyCollection<AuditRecord> AuditRecords
    );

    private sealed record LegacyBackupArchive(
        DateTimeOffset CreatedAt,
        string IntegrityHash,
        string CiphertextBase64,
        string? Algorithm,
        string? ManifestSignature,
        string? ManifestSignatureAlgorithm
    );

    private sealed record LegacyBackupPayload(
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<KeyRecord>? Keys,
        IReadOnlyCollection<ContactRecord>? Contacts,
        IReadOnlyCollection<AuditRecord>? AuditRecords
    );

}
