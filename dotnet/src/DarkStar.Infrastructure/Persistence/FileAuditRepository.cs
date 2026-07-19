using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using DarkStar.Application.Abstractions;
using DarkStar.Domain.Audit;
using DarkStar.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DarkStar.Infrastructure.Persistence;

public sealed class FileAuditRepository(IOptions<DarkStarStorageOptions> options) : IAuditRepository
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private string AuditPath => Path.Combine(options.Value.HomePath, "audit.jsonl");

    public async Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(options.Value.HomePath);
        var envelopes = await ReadEnvelopesAsync(cancellationToken);
        var prevHash = envelopes.Count == 0 ? "" : envelopes[^1].Hash;
        var hash = ComputeHash(prevHash, record);
        var envelope = new AuditEnvelope(record, prevHash, hash);
        var line = JsonSerializer.Serialize(envelope, _json);
        await File.AppendAllTextAsync(AuditPath, line + Environment.NewLine, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        var envelopes = await ReadEnvelopesAsync(cancellationToken);
        return envelopes.Select(e => e.Record).ToList();
    }

    public async Task ReplaceAllAsync(IReadOnlyCollection<AuditRecord> records, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(options.Value.HomePath);

        var prevHash = string.Empty;
        var lines = new List<string>(records.Count);
        foreach (var record in records)
        {
            var hash = ComputeHash(prevHash, record);
            var envelope = new AuditEnvelope(record, prevHash, hash);
            lines.Add(JsonSerializer.Serialize(envelope, _json));
            prevHash = hash;
        }

        var payload = lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines) + Environment.NewLine;
        await File.WriteAllTextAsync(AuditPath, payload, cancellationToken);
    }

    public async Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
    {
        var envelopes = await ReadEnvelopesAsync(cancellationToken);
        var prevHash = string.Empty;

        foreach (var envelope in envelopes)
        {
            if (!string.Equals(envelope.PrevHash, prevHash, StringComparison.Ordinal))
            {
                return false;
            }

            var computed = ComputeHash(envelope.PrevHash, envelope.Record);
            if (!string.Equals(computed, envelope.Hash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            prevHash = envelope.Hash;
        }

        return true;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var records = await ReadAllAsync(cancellationToken);
        return records.Count;
    }

    private async Task<List<AuditEnvelope>> ReadEnvelopesAsync(CancellationToken cancellationToken)
    {
        var results = new List<AuditEnvelope>();
        if (!File.Exists(AuditPath))
        {
            return results;
        }

        var lines = await File.ReadAllLinesAsync(AuditPath, cancellationToken);
        var prevHash = string.Empty;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var envelope = TryDeserializeEnvelope(line);
            if (envelope is not null)
            {
                results.Add(envelope);
                prevHash = envelope.Hash;
                continue;
            }

            // Backward compatibility with legacy line format containing AuditRecord only.
            var record = JsonSerializer.Deserialize<AuditRecord>(line, _json);
            if (record is null)
            {
                continue;
            }

            var hash = ComputeHash(prevHash, record);
            var legacyEnvelope = new AuditEnvelope(record, prevHash, hash);
            results.Add(legacyEnvelope);
            prevHash = hash;
        }

        return results;
    }

    private AuditEnvelope? TryDeserializeEnvelope(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AuditEnvelope>(json, _json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ComputeHash(string prevHash, AuditRecord record)
    {
        var recordJson = JsonSerializer.Serialize(record);
        var payload = Encoding.UTF8.GetBytes(prevHash + "|" + recordJson);
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record AuditEnvelope(AuditRecord Record, string PrevHash, string Hash);
}
