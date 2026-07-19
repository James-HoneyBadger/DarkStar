using DarkStar.Domain.Audit;

namespace DarkStar.Application.Abstractions;

public interface IAuditRepository
{
    Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditRecord>> ReadAllAsync(CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IReadOnlyCollection<AuditRecord> records, CancellationToken cancellationToken = default);
    Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
