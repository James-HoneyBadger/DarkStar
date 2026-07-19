using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using DarkStar.Domain.Audit;
using DarkStar.Infrastructure.Crypto;

namespace DarkStar.Domain.Tests;

public sealed class CryptoApplicationServiceTests
{
    [Fact]
    public async Task EncryptThenDecrypt_RoundTripPlaintext()
    {
        var service = BuildService();

        var encrypted = await service.EncryptTextAsync(new EncryptTextRequest("hello", "p@ss", "aes256gcm"));
        var decrypted = await service.DecryptTextAsync(
            new DecryptTextRequest(encrypted.CiphertextBase64, "p@ss", "aes256gcm")
        );

        Assert.Equal("hello", decrypted.Plaintext);
    }

    [Fact]
    public async Task SignThenVerify_ReturnsTrue()
    {
        var service = BuildService();

        var signed = await service.SignTextAsync(new SignTextRequest("payload", "secret"));
        var verified = await service.VerifyTextAsync(
            new VerifyTextRequest("payload", "secret", signed.SignatureBase64)
        );

        Assert.True(verified.IsValid);
    }

    [Fact]
    public async Task Verify_WithWrongSecret_ReturnsFalse()
    {
        var service = BuildService();

        var signed = await service.SignTextAsync(new SignTextRequest("payload", "secret-a"));
        var verified = await service.VerifyTextAsync(
            new VerifyTextRequest("payload", "secret-b", signed.SignatureBase64)
        );

        Assert.False(verified.IsValid);
    }

    private static CryptoApplicationService BuildService()
    {
        var engine = new AesGcmEncryptionEngine();
        var sig = new HmacSignatureEngine();
        var audit = new InMemoryAuditRepository();
        return new CryptoApplicationService(engine, sig, audit);
    }

    private sealed class InMemoryAuditRepository : IAuditRepository
    {
        private readonly List<AuditRecord> _records = [];

        public Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_records.Count);
        }

        public Task<IReadOnlyCollection<AuditRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<AuditRecord>>(_records.ToList());
        }

        public Task ReplaceAllAsync(IReadOnlyCollection<AuditRecord> records, CancellationToken cancellationToken = default)
        {
            _records.Clear();
            _records.AddRange(records);
            return Task.CompletedTask;
        }

        public Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
