using DarkStar.Domain.Keys;

namespace DarkStar.Application.Abstractions;

public interface IKeyRepository
{
    Task<IReadOnlyCollection<KeyRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<KeyRecord> AddAsync(KeyRecord key, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string fingerprint, CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IReadOnlyCollection<KeyRecord> keys, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
