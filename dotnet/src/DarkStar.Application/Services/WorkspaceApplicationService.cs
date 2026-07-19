using DarkStar.Application.Abstractions;
using DarkStar.Domain.Workspace;

namespace DarkStar.Application.Services;

public sealed class WorkspaceApplicationService(
    IKeyRepository keyRepository,
    IContactRepository contactRepository,
    IAuditRepository auditRepository)
{
    public async Task<WorkspaceSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var keyCount = await keyRepository.CountAsync(cancellationToken);
        var contactCount = await contactRepository.CountAsync(cancellationToken);
        var auditCount = await auditRepository.CountAsync(cancellationToken);

        return new WorkspaceSummary(keyCount, contactCount, auditCount);
    }
}
