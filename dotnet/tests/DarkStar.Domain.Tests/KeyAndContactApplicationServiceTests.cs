using DarkStar.Application.Abstractions;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using DarkStar.Domain.Audit;
using DarkStar.Domain.Contacts;
using DarkStar.Domain.Keys;
using DarkStar.Infrastructure.Options;
using DarkStar.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace DarkStar.Domain.Tests;

public sealed class KeyAndContactApplicationServiceTests
{
    [Fact]
    public async Task KeyAndContactLifecycle_UpdatesWorkspaceSummary()
    {
        var home = Path.Combine(Path.GetTempPath(), "darkstar-dotnet-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(home);

        var options = Options.Create(new DarkStarStorageOptions { HomePath = home });
        var keyRepo = new FileKeyRepository(options);
        var contactRepo = new FileContactRepository(options);
        var auditRepo = new InMemoryAuditRepository();

        var keyService = new KeyApplicationService(keyRepo, auditRepo);
        var contactService = new ContactApplicationService(contactRepo, auditRepo);
        var workspace = new WorkspaceApplicationService(keyRepo, contactRepo, auditRepo);

        var createdKey = await keyService.CreateAsync(new CreateKeyRequest("aes256gcm", "primary"));
        var createdContact = await contactService.CreateAsync(
            new CreateContactRequest("alice", "alice@example.com", "friend")
        );

        Assert.False(string.IsNullOrWhiteSpace(createdKey.Fingerprint));
        Assert.Equal("alice", createdContact.Name);

        var beforeDelete = await workspace.GetSummaryAsync();
        Assert.Equal(1, beforeDelete.KeyCount);
        Assert.Equal(1, beforeDelete.ContactCount);

        var keyDeleted = await keyService.DeleteAsync(createdKey.Fingerprint);
        var contactDeleted = await contactService.DeleteAsync(createdContact.Name);
        Assert.True(keyDeleted);
        Assert.True(contactDeleted);

        var afterDelete = await workspace.GetSummaryAsync();
        Assert.Equal(0, afterDelete.KeyCount);
        Assert.Equal(0, afterDelete.ContactCount);
    }

    private sealed class InMemoryAuditRepository : IAuditRepository
    {
        private readonly List<AuditRecord> _records = [];

        public Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_records.Count);
        }

        public Task<IReadOnlyCollection<AuditRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<AuditRecord>>(_records.ToList());
        }

        public Task ReplaceAllAsync(IReadOnlyCollection<AuditRecord> records, CancellationToken cancellationToken = default)
        {
            _records.Clear();
            _records.AddRange(records);
            return Task.CompletedTask;
        }

        public Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
