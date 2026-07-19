using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DarkStar.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DarkStar.Api.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EncryptThenDecryptText_RoundTrips()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var encResponse = await client.PostAsJsonAsync(
            "/api/crypto/encrypt-text",
            new EncryptTextApiRequest("hello integration", "passphrase", "aes256gcm")
        );
        encResponse.EnsureSuccessStatusCode();
        var encPayload = await encResponse.Content.ReadFromJsonAsync<EncryptTextApiResponse>();
        Assert.NotNull(encPayload);

        var decResponse = await client.PostAsJsonAsync(
            "/api/crypto/decrypt-text",
            new DecryptTextApiRequest(encPayload!.CiphertextBase64, "passphrase", "aes256gcm")
        );
        decResponse.EnsureSuccessStatusCode();
        var decPayload = await decResponse.Content.ReadFromJsonAsync<DecryptTextApiResponse>();

        Assert.NotNull(decPayload);
        Assert.Equal("hello integration", decPayload!.Plaintext);
    }

    [Fact]
    public async Task EncryptThenDecryptFile_RoundTrips()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var home = Path.Combine(Path.GetTempPath(), "darkstar-api-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(home);

        var inputPath = Path.Combine(home, "input.txt");
        var encryptedPath = Path.Combine(home, "input.txt.dstar");
        var outputPath = Path.Combine(home, "output.txt");

        await File.WriteAllTextAsync(inputPath, "file payload");

        var encResponse = await client.PostAsJsonAsync(
            "/api/crypto/encrypt-file",
            new EncryptFileApiRequest(inputPath, encryptedPath, "passphrase", "aes256gcm")
        );
        encResponse.EnsureSuccessStatusCode();

        Assert.True(File.Exists(encryptedPath));

        var decResponse = await client.PostAsJsonAsync(
            "/api/crypto/decrypt-file",
            new DecryptFileApiRequest(encryptedPath, outputPath, "passphrase", "aes256gcm")
        );
        decResponse.EnsureSuccessStatusCode();

        Assert.True(File.Exists(outputPath));
        var outputText = await File.ReadAllTextAsync(outputPath);
        Assert.Equal("file payload", outputText);
    }

    [Fact]
    public async Task KeyAndContactCrud_UpdatesWorkspaceSummary()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var keyCreate = await client.PostAsJsonAsync(
            "/api/keys",
            new CreateKeyApiRequest("aes256gcm", "primary")
        );
        keyCreate.EnsureSuccessStatusCode();
        var key = await keyCreate.Content.ReadFromJsonAsync<JsonElement>();
        var fingerprint = key.GetProperty("fingerprint").GetString();
        Assert.False(string.IsNullOrWhiteSpace(fingerprint));

        var contactCreate = await client.PostAsJsonAsync(
            "/api/contacts",
            new CreateContactApiRequest("alice", "alice@example.com", "friend")
        );
        contactCreate.EnsureSuccessStatusCode();

        var summaryResponse = await client.GetAsync("/api/workspace/summary");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(summary.GetProperty("keyCount").GetInt32() >= 1);
        Assert.True(summary.GetProperty("contactCount").GetInt32() >= 1);

        var keyDelete = await client.DeleteAsync($"/api/keys/{fingerprint}");
        keyDelete.EnsureSuccessStatusCode();

        var contactDelete = await client.DeleteAsync("/api/contacts/alice");
        contactDelete.EnsureSuccessStatusCode();
    }

    private static WebApplicationFactory<Program> BuildFactory()
    {
        var home = Path.Combine(Path.GetTempPath(), "darkstar-api-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(home);
        Environment.SetEnvironmentVariable("DARKSTAR_HOME", home);

        return new WebApplicationFactory<Program>();
    }
}
