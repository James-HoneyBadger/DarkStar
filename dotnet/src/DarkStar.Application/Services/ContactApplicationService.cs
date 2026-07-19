using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Domain.Audit;
using DarkStar.Domain.Contacts;

namespace DarkStar.Application.Services;

public sealed class ContactApplicationService(
    IContactRepository contactRepository,
    IAuditRepository auditRepository)
{
    public Task<IReadOnlyCollection<ContactRecord>> ListAsync(CancellationToken cancellationToken = default)
        => contactRepository.ListAsync(cancellationToken);

    public async Task<ContactRecord> CreateAsync(
        CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Contact name is required", nameof(request));
        }

        var created = new ContactRecord(
            request.Name.Trim(),
            request.Email?.Trim(),
            request.Notes?.Trim(),
            DateTimeOffset.UtcNow
        );

        var stored = await contactRepository.AddAsync(created, cancellationToken);

        await auditRepository.AppendAsync(
            new AuditRecord(
                DateTimeOffset.UtcNow,
                "contact_create",
                stored.Name,
                stored.Email ?? string.Empty
            ),
            cancellationToken
        );

        return stored;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Contact name is required", nameof(name));
        }

        var deleted = await contactRepository.RemoveAsync(name.Trim(), cancellationToken);
        if (deleted)
        {
            await auditRepository.AppendAsync(
                new AuditRecord(DateTimeOffset.UtcNow, "contact_delete", name.Trim(), string.Empty),
                cancellationToken
            );
        }

        return deleted;
    }
}
