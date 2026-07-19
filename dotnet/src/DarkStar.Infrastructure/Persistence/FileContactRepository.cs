using System.Text.Json;
using DarkStar.Application.Abstractions;
using DarkStar.Domain.Contacts;
using DarkStar.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DarkStar.Infrastructure.Persistence;

public sealed class FileContactRepository(IOptions<DarkStarStorageOptions> options) : IContactRepository
{
    private string ContactPath => Path.Combine(options.Value.HomePath, "contacts.json");

    public async Task<IReadOnlyCollection<ContactRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ContactPath))
        {
            return Array.Empty<ContactRecord>();
        }

        await using var stream = File.OpenRead(ContactPath);
        var contacts = await JsonSerializer.DeserializeAsync<List<ContactRecord>>(stream, cancellationToken: cancellationToken);
        if (contacts is null)
        {
            return Array.Empty<ContactRecord>();
        }

        return contacts;
    }

    public async Task<ContactRecord> AddAsync(ContactRecord contact, CancellationToken cancellationToken = default)
    {
        var contacts = (await ListAsync(cancellationToken)).ToList();
        var existing = contacts.FirstOrDefault(c =>
            string.Equals(c.Name, contact.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            throw new InvalidOperationException($"Contact {contact.Name} already exists");
        }

        contacts.Add(contact);
        await SaveAllAsync(contacts, cancellationToken);
        return contact;
    }

    public async Task<bool> RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        var contacts = (await ListAsync(cancellationToken)).ToList();
        var removed = contacts.RemoveAll(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return false;
        }

        await SaveAllAsync(contacts, cancellationToken);
        return true;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var contacts = await ListAsync(cancellationToken);
        return contacts.Count;
    }

    public async Task ReplaceAllAsync(IReadOnlyCollection<ContactRecord> contacts, CancellationToken cancellationToken = default)
    {
        await SaveAllAsync(contacts, cancellationToken);
    }

    private async Task SaveAllAsync(IReadOnlyCollection<ContactRecord> contacts, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.Value.HomePath);
        var tmpPath = ContactPath + ".tmp";

        await using (var stream = File.Create(tmpPath))
        {
            await JsonSerializer.SerializeAsync(stream, contacts, cancellationToken: cancellationToken);
        }

        File.Move(tmpPath, ContactPath, overwrite: true);
    }
}
