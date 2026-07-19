using System.Net.Http.Json;
using System.Text.Json;
using DarkStar.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DarkStar.Api.Tests;

public sealed class BackupAndAuditIntegrationTests
{
    [Fact]
    public async Task BackupCreateThenVerifyThenRestore_RoundTripsWorkspace()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/keys", new CreateKeyApiRequest("aes256gcm", "k1"));
        await client.PostAsJsonAsync("/api/contacts", new CreateContactApiRequest("alice", "a@example.com", "n"));

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "backup.dstar.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        var createResponse = await client.PostAsJsonAsync(
            "/api/backup/create",
            new CreateBackupRequestDto(backupPath, "passphrase", "aes256gcm", "signing-secret")
        );
        createResponse.EnsureSuccessStatusCode();
        var createPayload = await createResponse.Content.ReadFromJsonAsync<CreateBackupResponseDto>();
        Assert.NotNull(createPayload);
        Assert.False(string.IsNullOrWhiteSpace(createPayload!.ManifestSignature));

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/backup/verify",
            new VerifyBackupRequestDto(backupPath, "passphrase", "aes256gcm", "signing-secret")
        );
        verifyResponse.EnsureSuccessStatusCode();
        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<VerifyBackupResponseDto>();
        Assert.NotNull(verifyPayload);
        Assert.True(verifyPayload!.IsValid);
        Assert.True(verifyPayload.IsSignaturePresent);
        Assert.True(verifyPayload.IsSignatureValid);
        Assert.Equal("hmac-sha256", verifyPayload.ManifestSignatureAlgorithm);

        var deleteKey = await client.GetFromJsonAsync<List<object>>("/api/keys");
        Assert.NotNull(deleteKey);

        var keyList = await client.GetFromJsonAsync<List<KeyView>>("/api/keys");
        Assert.NotNull(keyList);
        if (keyList!.Count > 0)
        {
            var delResponse = await client.DeleteAsync($"/api/keys/{keyList[0].Fingerprint}");
            delResponse.EnsureSuccessStatusCode();
        }

        var contactDel = await client.DeleteAsync("/api/contacts/alice");
        contactDel.EnsureSuccessStatusCode();

        var restoreResponse = await client.PostAsJsonAsync(
            "/api/backup/restore",
            new RestoreBackupRequestDto(backupPath, "passphrase", "aes256gcm", "signing-secret")
        );
        restoreResponse.EnsureSuccessStatusCode();
        var restorePayload = await restoreResponse.Content.ReadFromJsonAsync<RestoreBackupResponseDto>();
        Assert.NotNull(restorePayload);
        Assert.True(restorePayload!.SignatureVerified);
        Assert.Equal("hmac-sha256", restorePayload.ManifestSignatureAlgorithm);

        var summary = await client.GetFromJsonAsync<WorkspaceSummaryView>("/api/workspace/summary");
        Assert.NotNull(summary);
        Assert.True(summary!.KeyCount >= 1);
        Assert.True(summary.ContactCount >= 1);
    }

    [Fact]
    public async Task AuditVerify_DetectsTamper()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/crypto/encrypt-text", new EncryptTextApiRequest("hello", "passphrase", "aes256gcm"));

        var validResponse = await client.GetFromJsonAsync<AuditVerifyResponseDto>("/api/audit/verify");
        Assert.NotNull(validResponse);
        Assert.True(validResponse!.IsValid);

        var home = Environment.GetEnvironmentVariable("DARKSTAR_HOME")!;
        var auditPath = Path.Combine(home, "audit.jsonl");
        var lines = await File.ReadAllLinesAsync(auditPath);
        Assert.NotEmpty(lines);

        using var doc = JsonDocument.Parse(lines[^1]);
        var root = doc.RootElement;
        var record = root.GetProperty("record").GetRawText();
        var prevHash = root.GetProperty("prevHash").GetString() ?? string.Empty;
        var hash = root.GetProperty("hash").GetString() ?? string.Empty;
        var tamperedHash = hash.Length == 0
            ? "0"
            : ((hash[0] == '0' ? '1' : '0') + hash[1..]);

        lines[^1] = $"{{\"record\":{record},\"prevHash\":\"{prevHash}\",\"hash\":\"{tamperedHash}\"}}";
        await File.WriteAllLinesAsync(auditPath, lines);

        var invalidResponse = await client.GetFromJsonAsync<AuditVerifyResponseDto>("/api/audit/verify");
        Assert.NotNull(invalidResponse);
        Assert.False(invalidResponse!.IsValid);
    }

    [Fact]
    public async Task BackupCreateVerifyRestore_WithRsaManifestSignature_Works()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privatePem = rsa.ExportRSAPrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        var backupPath = Path.Combine(Path.GetTempPath(), "darkstar-backup-tests", Guid.NewGuid().ToString("n"), "backup-rsa.dstar.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        var createResponse = await client.PostAsJsonAsync(
            "/api/backup/create",
            new CreateBackupRequestDto(
                backupPath,
                "passphrase",
                "aes256gcm",
                SigningPrivateKeyPem: privatePem,
                SignatureMode: "rsa")
        );
        createResponse.EnsureSuccessStatusCode();

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/backup/verify",
            new VerifyBackupRequestDto(
                backupPath,
                "passphrase",
                "aes256gcm",
                SigningPublicKeyPem: publicPem,
                SignatureMode: "rsa")
        );
        verifyResponse.EnsureSuccessStatusCode();
        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<VerifyBackupResponseDto>();
        Assert.NotNull(verifyPayload);
        Assert.True(verifyPayload!.IsValid);
        Assert.True(verifyPayload.IsSignatureValid);
        Assert.Equal("rsa-pss-sha256", verifyPayload.ManifestSignatureAlgorithm);

        var restoreResponse = await client.PostAsJsonAsync(
            "/api/backup/restore",
            new RestoreBackupRequestDto(
                backupPath,
                "passphrase",
                "aes256gcm",
                SigningPublicKeyPem: publicPem,
                SignatureMode: "rsa")
        );
        restoreResponse.EnsureSuccessStatusCode();
        var restorePayload = await restoreResponse.Content.ReadFromJsonAsync<RestoreBackupResponseDto>();
        Assert.NotNull(restorePayload);
        Assert.True(restorePayload!.SignatureVerified);
        Assert.Equal("rsa-pss-sha256", restorePayload.ManifestSignatureAlgorithm);
    }

    private static WebApplicationFactory<Program> BuildFactory()
    {
        var home = Path.Combine(Path.GetTempPath(), "darkstar-api-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(home);
        Environment.SetEnvironmentVariable("DARKSTAR_HOME", home);

        return new WebApplicationFactory<Program>();
    }

    private sealed record KeyView(string Fingerprint);
    private sealed record WorkspaceSummaryView(int KeyCount, int ContactCount, int AuditCount);
}
