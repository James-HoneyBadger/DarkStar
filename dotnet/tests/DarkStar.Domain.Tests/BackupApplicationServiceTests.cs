using System.Security.Cryptography;
using System.Text.Json;
using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using DarkStar.Domain.Audit;
using DarkStar.Domain.Contacts;
using DarkStar.Domain.Keys;
using DarkStar.Domain.Security;
using DarkStar.Infrastructure.Crypto;

namespace DarkStar.Domain.Tests;

public sealed class BackupApplicationServiceTests
{
    [Fact]
    public async Task VerifyBackup_LegacyUnversionedArchive_IsAcceptedViaMigration()
    {
        var service = BuildService(out _, out _, out _);

        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Keys = Array.Empty<KeyRecord>(),
            Contacts = Array.Empty<ContactRecord>(),
            AuditRecords = Array.Empty<AuditRecord>()
        });

        var algorithm = CipherAlgorithm.Aes256Gcm;
        var passphrase = "passphrase";
        var engine = new AesGcmEncryptionEngine();
        var ciphertext = engine.EncryptBytes(payloadJson, passphrase, algorithm);
        var integrity = ComputeHexSha256(ciphertext);

        var legacyArchive = new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            IntegrityHash = integrity,
            CiphertextBase64 = Convert.ToBase64String(ciphertext)
        };

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "legacy.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await File.WriteAllBytesAsync(backupPath, JsonSerializer.SerializeToUtf8Bytes(legacyArchive));

        var result = await service.VerifyBackupAsync(new VerifyBackupRequest(backupPath, passphrase, "aes256gcm"));

        Assert.True(result.IsValid);
        Assert.False(result.IsSignaturePresent);
        Assert.False(result.IsSignatureValid);
        Assert.Equal(0, result.KeyCount);
        Assert.Equal(0, result.ContactCount);
        Assert.Equal(0, result.AuditCount);
    }

    [Fact]
    public async Task VerifyBackup_SignedArchive_WithMatchingSecret_IsValid()
    {
        var service = BuildService(out _, out _, out _);

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "signed.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        var create = await service.CreateBackupAsync(new CreateBackupRequest(
            backupPath,
            "passphrase",
            "aes256gcm",
            SigningSecret: "signing-secret"));

        Assert.False(string.IsNullOrWhiteSpace(create.ManifestSignature));

        var verify = await service.VerifyBackupAsync(new VerifyBackupRequest(
            backupPath,
            "passphrase",
            "aes256gcm",
            SigningSecret: "signing-secret"));

        Assert.True(verify.IsValid);
        Assert.True(verify.IsSignaturePresent);
        Assert.True(verify.IsSignatureValid);
        Assert.Equal("hmac-sha256", verify.ManifestSignatureAlgorithm);
    }

    [Fact]
    public async Task VerifyBackup_RsaSignedArchive_WithMatchingPublicKey_IsValid()
    {
        var service = BuildService(out _, out _, out _);

        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportRSAPrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "signed-rsa.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        var create = await service.CreateBackupAsync(new CreateBackupRequest(
            backupPath,
            "passphrase",
            "aes256gcm",
            SigningPrivateKeyPem: privatePem,
            SignatureMode: "rsa"));

        Assert.False(string.IsNullOrWhiteSpace(create.ManifestSignature));
        Assert.Equal("rsa-pss-sha256", create.ManifestSignatureAlgorithm);

        var verify = await service.VerifyBackupAsync(new VerifyBackupRequest(
            backupPath,
            "passphrase",
            "aes256gcm",
            SigningPublicKeyPem: publicPem,
            SignatureMode: "rsa"));

        Assert.True(verify.IsValid);
        Assert.True(verify.IsSignaturePresent);
        Assert.True(verify.IsSignatureValid);
        Assert.Equal("rsa-pss-sha256", verify.ManifestSignatureAlgorithm);
    }

    [Fact]
    public async Task RestoreBackup_RsaSignedArchive_WithWrongPublicKey_Throws()
    {
        var service = BuildService(out _, out _, out _);

        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportRSAPrivateKeyPem();
        using var wrongRsa = RSA.Create(2048);
        var wrongPublicPem = wrongRsa.ExportSubjectPublicKeyInfoPem();

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "signed-rsa-wrong.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        await service.CreateBackupAsync(new CreateBackupRequest(
            backupPath,
            "passphrase",
            "aes256gcm",
            SigningPrivateKeyPem: privatePem,
            SignatureMode: "rsa"));

        await Assert.ThrowsAsync<CryptographicException>(
            () => service.RestoreBackupAsync(new RestoreBackupRequest(
                backupPath,
                "passphrase",
                "aes256gcm",
                SigningPublicKeyPem: wrongPublicPem,
                SignatureMode: "rsa"))
        );
    }

    [Fact]
    public async Task RestoreBackup_SignedArchive_WithoutSecret_Throws()
    {
        var service = BuildService(out _, out _, out _);

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "signed-restore.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        await service.CreateBackupAsync(new CreateBackupRequest(
            backupPath,
            "passphrase",
            "aes256gcm",
            SigningSecret: "signing-secret"));

        await Assert.ThrowsAsync<CryptographicException>(
            () => service.RestoreBackupAsync(new RestoreBackupRequest(backupPath, "passphrase", "aes256gcm"))
        );
    }

    [Fact]
    public async Task VerifyBackup_FutureArchiveVersion_ThrowsNotSupported()
    {
        var service = BuildService(out _, out _, out _);

        var passphrase = "passphrase";
        var algorithm = CipherAlgorithm.Aes256Gcm;
        var engine = new AesGcmEncryptionEngine();

        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Keys = Array.Empty<KeyRecord>(),
            Contacts = Array.Empty<ContactRecord>(),
            AuditRecords = Array.Empty<AuditRecord>()
        });

        var ciphertext = engine.EncryptBytes(payloadJson, passphrase, algorithm);
        var archive = new
        {
            Version = 2,
            Algorithm = "Aes256Gcm",
            CreatedAt = DateTimeOffset.UtcNow,
            IntegrityHash = ComputeHexSha256(ciphertext),
            CiphertextBase64 = Convert.ToBase64String(ciphertext)
        };

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "future.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await File.WriteAllBytesAsync(backupPath, JsonSerializer.SerializeToUtf8Bytes(archive));

        await Assert.ThrowsAsync<NotSupportedException>(
            () => service.VerifyBackupAsync(new VerifyBackupRequest(backupPath, passphrase, "aes256gcm"))
        );
    }

    [Fact]
    public async Task VerifyBackup_ArchiveWithUnknownField_ThrowsInvalidData()
    {
        var service = BuildService(out _, out _, out _);

        var passphrase = "passphrase";
        var algorithm = CipherAlgorithm.Aes256Gcm;
        var engine = new AesGcmEncryptionEngine();

        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Keys = Array.Empty<KeyRecord>(),
            Contacts = Array.Empty<ContactRecord>(),
            AuditRecords = Array.Empty<AuditRecord>()
        });

        var ciphertext = engine.EncryptBytes(payloadJson, passphrase, algorithm);
        var archive = new
        {
            Version = 1,
            Algorithm = "Aes256Gcm",
            CreatedAt = DateTimeOffset.UtcNow,
            IntegrityHash = ComputeHexSha256(ciphertext),
            CiphertextBase64 = Convert.ToBase64String(ciphertext),
            Unexpected = "x"
        };

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "unknown-field.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await File.WriteAllBytesAsync(backupPath, JsonSerializer.SerializeToUtf8Bytes(archive));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.VerifyBackupAsync(new VerifyBackupRequest(backupPath, passphrase, "aes256gcm"))
        );
    }

    private static BackupApplicationService BuildService(
        out InMemoryKeyRepository keyRepository,
        out InMemoryContactRepository contactRepository,
        out InMemoryAuditRepository auditRepository)
    {
        var engine = new AesGcmEncryptionEngine();
        var signatureEngine = new HmacSignatureEngine();
        keyRepository = new InMemoryKeyRepository();
        contactRepository = new InMemoryContactRepository();
        auditRepository = new InMemoryAuditRepository();

        return new BackupApplicationService(engine, signatureEngine, keyRepository, contactRepository, auditRepository);
    }

    private static string ComputeHexSha256(ReadOnlySpan<byte> payload)
    {
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class InMemoryKeyRepository : IKeyRepository
    {
        private readonly List<KeyRecord> _keys = [];

        public Task<IReadOnlyCollection<KeyRecord>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<KeyRecord>>(_keys.ToList());

        public Task<KeyRecord> AddAsync(KeyRecord key, CancellationToken cancellationToken = default)
        {
            _keys.Add(key);
            return Task.FromResult(key);
        }

        public Task<bool> RemoveAsync(string fingerprint, CancellationToken cancellationToken = default)
        {
            var removed = _keys.RemoveAll(x => string.Equals(x.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(removed > 0);
        }

        public Task ReplaceAllAsync(IReadOnlyCollection<KeyRecord> keys, CancellationToken cancellationToken = default)
        {
            _keys.Clear();
            _keys.AddRange(keys);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_keys.Count);
    }

    private sealed class InMemoryContactRepository : IContactRepository
    {
        private readonly List<ContactRecord> _contacts = [];

        public Task<IReadOnlyCollection<ContactRecord>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ContactRecord>>(_contacts.ToList());

        public Task<ContactRecord> AddAsync(ContactRecord contact, CancellationToken cancellationToken = default)
        {
            _contacts.Add(contact);
            return Task.FromResult(contact);
        }

        public Task<bool> RemoveAsync(string name, CancellationToken cancellationToken = default)
        {
            var removed = _contacts.RemoveAll(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(removed > 0);
        }

        public Task ReplaceAllAsync(IReadOnlyCollection<ContactRecord> contacts, CancellationToken cancellationToken = default)
        {
            _contacts.Clear();
            _contacts.AddRange(contacts);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_contacts.Count);
    }

    private sealed class InMemoryAuditRepository : IAuditRepository
    {
        private readonly List<AuditRecord> _records = [];

        public Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AuditRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AuditRecord>>(_records.ToList());

        public Task ReplaceAllAsync(IReadOnlyCollection<AuditRecord> records, CancellationToken cancellationToken = default)
        {
            _records.Clear();
            _records.AddRange(records);
            return Task.CompletedTask;
        }

        public Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_records.Count);
    }
}
