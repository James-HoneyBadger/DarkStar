using DarkStar.Application.Abstractions;

namespace DarkStar.Application.Services;

public sealed class AuditApplicationService(IAuditRepository auditRepository)
{
    public Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
        => auditRepository.VerifyIntegrityAsync(cancellationToken);
}
