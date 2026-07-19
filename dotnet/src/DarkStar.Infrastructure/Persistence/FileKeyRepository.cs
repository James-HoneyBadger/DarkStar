using System.Text.Json;
using DarkStar.Application.Abstractions;
using DarkStar.Domain.Keys;
using DarkStar.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DarkStar.Infrastructure.Persistence;

public sealed class FileKeyRepository(IOptions<DarkStarStorageOptions> options) : IKeyRepository
{
    private string KeyPath => Path.Combine(options.Value.HomePath, "keys.json");

    public async Task<IReadOnlyCollection<KeyRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(KeyPath))
        {
            return Array.Empty<KeyRecord>();
        }

        await using var stream = File.OpenRead(KeyPath);
        var keys = await JsonSerializer.DeserializeAsync<List<KeyRecord>>(stream, cancellationToken: cancellationToken);
        if (keys is null)
        {
            return Array.Empty<KeyRecord>();
        }

        return keys;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var keys = await ListAsync(cancellationToken);
        return keys.Count;
    }

    public async Task ReplaceAllAsync(IReadOnlyCollection<KeyRecord> keys, CancellationToken cancellationToken = default)
    {
        await SaveAllAsync(keys, cancellationToken);
    }

    public async Task<KeyRecord> AddAsync(KeyRecord key, CancellationToken cancellationToken = default)
    {
        var keys = (await ListMutableAsync(cancellationToken)).ToList();
        var existing = keys.FirstOrDefault(k =>
            string.Equals(k.Fingerprint, key.Fingerprint, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            throw new InvalidOperationException($"Key {key.Fingerprint} already exists");
        }

        keys.Add(key);
        await SaveAllAsync(keys, cancellationToken);
        return key;
    }

    public async Task<bool> RemoveAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        var keys = (await ListMutableAsync(cancellationToken)).ToList();
        var removed = keys.RemoveAll(k =>
            string.Equals(k.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
        {
            return false;
        }

        await SaveAllAsync(keys, cancellationToken);
        return true;
    }

    private async Task<IReadOnlyCollection<KeyRecord>> ListMutableAsync(CancellationToken cancellationToken)
    {
        return await ListAsync(cancellationToken);
    }

    private async Task SaveAllAsync(IReadOnlyCollection<KeyRecord> keys, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.Value.HomePath);
        var tmpPath = KeyPath + ".tmp";

        await using (var stream = File.Create(tmpPath))
        {
            await JsonSerializer.SerializeAsync(stream, keys, cancellationToken: cancellationToken);
        }

        File.Move(tmpPath, KeyPath, overwrite: true);
    }
}
