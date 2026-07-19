using DarkStar.Domain.Contacts;

namespace DarkStar.Application.Abstractions;

public interface IContactRepository
{
    Task<IReadOnlyCollection<ContactRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<ContactRecord> AddAsync(ContactRecord contact, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string name, CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IReadOnlyCollection<ContactRecord> contacts, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
