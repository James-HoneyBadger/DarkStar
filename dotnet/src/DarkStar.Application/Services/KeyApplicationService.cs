using System.Security.Cryptography;
using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Domain.Audit;
using DarkStar.Domain.Keys;
using DarkStar.Domain.Security;

namespace DarkStar.Application.Services;

public sealed class KeyApplicationService(
    IKeyRepository keyRepository,
    IAuditRepository auditRepository)
{
    public Task<IReadOnlyCollection<KeyRecord>> ListAsync(CancellationToken cancellationToken = default)
        => keyRepository.ListAsync(cancellationToken);

    public async Task<CreateKeyResult> CreateAsync(
        CreateKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
        {
            throw new ArgumentException("Label is required", nameof(request));
        }

        var parsed = CipherAlgorithmParser.Parse(request.Algorithm);
        var fingerprint = GenerateFingerprint();
        var createdAt = DateTimeOffset.UtcNow;

        var key = new KeyRecord(
            fingerprint,
            parsed.ToString(),
            request.Label.Trim(),
            createdAt
        );

        await keyRepository.AddAsync(key, cancellationToken);

        await auditRepository.AppendAsync(
            new AuditRecord(
                createdAt,
                "key_create",
                key.Fingerprint,
                $"algorithm={key.Algorithm};label={key.Label}"
            ),
            cancellationToken
        );

        return new CreateKeyResult(key.Fingerprint, key.Algorithm, key.Label, key.CreatedAt);
    }

    public async Task<bool> DeleteAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("Fingerprint is required", nameof(fingerprint));
        }

        var deleted = await keyRepository.RemoveAsync(fingerprint.Trim(), cancellationToken);
        if (deleted)
        {
            await auditRepository.AppendAsync(
                new AuditRecord(DateTimeOffset.UtcNow, "key_delete", fingerprint.Trim(), string.Empty),
                cancellationToken
            );
        }

        return deleted;
    }

    private static string GenerateFingerprint()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
