using DarkStar.Application;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using DarkStar.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", optional: true)
	.AddEnvironmentVariables(prefix: "DARKSTAR_")
	.Build();

var services = new ServiceCollection()
	.AddDarkStarApplication()
	.AddDarkStarInfrastructure(configuration)
	.BuildServiceProvider();

if (args.Length == 0)
{
	PrintUsage();
	return;
}

var command = args[0].Trim().ToLowerInvariant();

switch (command)
{
	case "encrypt-text":
		await RunEncryptTextAsync(args.Skip(1).ToArray(), services);
		break;
	case "encrypt-file":
		await RunEncryptFileAsync(args.Skip(1).ToArray(), services);
		break;
	case "decrypt-text":
		await RunDecryptTextAsync(args.Skip(1).ToArray(), services);
		break;
	case "decrypt-file":
		await RunDecryptFileAsync(args.Skip(1).ToArray(), services);
		break;
	case "sign-text":
		await RunSignTextAsync(args.Skip(1).ToArray(), services);
		break;
	case "verify-text":
		await RunVerifyTextAsync(args.Skip(1).ToArray(), services);
		break;
	case "workspace-summary":
		await RunWorkspaceSummaryAsync(services);
		break;
	case "create-key":
		await RunCreateKeyAsync(args.Skip(1).ToArray(), services);
		break;
	case "list-keys":
		await RunListKeysAsync(services);
		break;
	case "delete-key":
		await RunDeleteKeyAsync(args.Skip(1).ToArray(), services);
		break;
	case "add-contact":
		await RunAddContactAsync(args.Skip(1).ToArray(), services);
		break;
	case "list-contacts":
		await RunListContactsAsync(services);
		break;
	case "delete-contact":
		await RunDeleteContactAsync(args.Skip(1).ToArray(), services);
		break;
	case "audit-list":
		await RunAuditListAsync(services);
		break;
	case "audit-verify":
		await RunAuditVerifyAsync(services);
		break;
	case "backup-create":
		await RunBackupCreateAsync(args.Skip(1).ToArray(), services);
		break;
	case "backup-verify":
		await RunBackupVerifyAsync(args.Skip(1).ToArray(), services);
		break;
	case "backup-restore":
		await RunBackupRestoreAsync(args.Skip(1).ToArray(), services);
		break;
	default:
		PrintUsage();
		break;
}

return;

static async Task RunEncryptTextAsync(string[] args, ServiceProvider services)
{
	var text = GetValue(args, "--text");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";

	if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --text and --passphrase");
		return;
	}

	var crypto = services.GetRequiredService<CryptoApplicationService>();
	var result = await crypto.EncryptTextAsync(new EncryptTextRequest(text, passphrase, algorithm));

	Console.WriteLine("Encryption successful");
	Console.WriteLine($"Algorithm : {result.Algorithm}");
	Console.WriteLine($"Ciphertext: {result.CiphertextBase64}");
}

static async Task RunEncryptFileAsync(string[] args, ServiceProvider services)
{
	var input = GetValue(args, "--input");
	var output = GetValue(args, "--output");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";

	if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --input and --passphrase");
		return;
	}

	var crypto = services.GetRequiredService<CryptoApplicationService>();
	var result = await crypto.EncryptFileAsync(new EncryptFileRequest(input, output, passphrase, algorithm));

	Console.WriteLine("File encryption successful");
	Console.WriteLine($"Input : {result.InputPath}");
	Console.WriteLine($"Output: {result.OutputPath}");
	Console.WriteLine($"Bytes : {result.InputBytes} -> {result.OutputBytes}");
}

static async Task RunDecryptTextAsync(string[] args, ServiceProvider services)
{
	var ciphertext = GetValue(args, "--ciphertext");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";

	if (string.IsNullOrWhiteSpace(ciphertext) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --ciphertext and --passphrase");
		return;
	}

	var crypto = services.GetRequiredService<CryptoApplicationService>();
	var result = await crypto.DecryptTextAsync(new DecryptTextRequest(ciphertext, passphrase, algorithm));

	Console.WriteLine("Decryption successful");
	Console.WriteLine($"Algorithm: {result.Algorithm}");
	Console.WriteLine($"Plaintext: {result.Plaintext}");
}

static async Task RunDecryptFileAsync(string[] args, ServiceProvider services)
{
	var input = GetValue(args, "--input");
	var output = GetValue(args, "--output");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";

	if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --input and --passphrase");
		return;
	}

	var crypto = services.GetRequiredService<CryptoApplicationService>();
	var result = await crypto.DecryptFileAsync(new DecryptFileRequest(input, output, passphrase, algorithm));

	Console.WriteLine("File decryption successful");
	Console.WriteLine($"Input : {result.InputPath}");
	Console.WriteLine($"Output: {result.OutputPath}");
	Console.WriteLine($"Bytes : {result.InputBytes} -> {result.OutputBytes}");
}

static async Task RunSignTextAsync(string[] args, ServiceProvider services)
{
	var message = GetValue(args, "--message");
	var secret = GetValue(args, "--secret");

	if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(secret))
	{
		Console.Error.WriteLine("Missing required options: --message and --secret");
		return;
	}

	var crypto = services.GetRequiredService<CryptoApplicationService>();
	var result = await crypto.SignTextAsync(new SignTextRequest(message, secret));

	Console.WriteLine("Signing successful");
	Console.WriteLine($"Signature: {result.SignatureBase64}");
}

static async Task RunVerifyTextAsync(string[] args, ServiceProvider services)
{
	var message = GetValue(args, "--message");
	var secret = GetValue(args, "--secret");
	var signature = GetValue(args, "--signature");

	if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signature))
	{
		Console.Error.WriteLine("Missing required options: --message, --secret, and --signature");
		return;
	}

	var crypto = services.GetRequiredService<CryptoApplicationService>();
	var result = await crypto.VerifyTextAsync(new VerifyTextRequest(message, secret, signature));

	Console.WriteLine($"Signature valid: {result.IsValid}");
}

static async Task RunWorkspaceSummaryAsync(ServiceProvider services)
{
	var workspace = services.GetRequiredService<WorkspaceApplicationService>();
	var summary = await workspace.GetSummaryAsync();

	Console.WriteLine("DarkStar Workspace Summary");
	Console.WriteLine($"Keys      : {summary.KeyCount}");
	Console.WriteLine($"Contacts  : {summary.ContactCount}");
	Console.WriteLine($"Audit Logs: {summary.AuditCount}");
}

static async Task RunCreateKeyAsync(string[] args, ServiceProvider services)
{
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";
	var label = GetValue(args, "--label");

	if (string.IsNullOrWhiteSpace(label))
	{
		Console.Error.WriteLine("Missing required option: --label");
		return;
	}

	var keyService = services.GetRequiredService<KeyApplicationService>();
	var created = await keyService.CreateAsync(new CreateKeyRequest(algorithm, label));

	Console.WriteLine("Key created");
	Console.WriteLine($"Fingerprint: {created.Fingerprint}");
	Console.WriteLine($"Algorithm  : {created.Algorithm}");
	Console.WriteLine($"Label      : {created.Label}");
}

static async Task RunListKeysAsync(ServiceProvider services)
{
	var keyService = services.GetRequiredService<KeyApplicationService>();
	var keys = await keyService.ListAsync();

	foreach (var key in keys)
	{
		Console.WriteLine($"{key.Fingerprint}  {key.Algorithm}  {key.Label}");
	}
}

static async Task RunDeleteKeyAsync(string[] args, ServiceProvider services)
{
	var fingerprint = GetValue(args, "--fingerprint");
	if (string.IsNullOrWhiteSpace(fingerprint))
	{
		Console.Error.WriteLine("Missing required option: --fingerprint");
		return;
	}

	var keyService = services.GetRequiredService<KeyApplicationService>();
	var deleted = await keyService.DeleteAsync(fingerprint);
	Console.WriteLine(deleted ? "Key deleted" : "Key not found");
}

static async Task RunAddContactAsync(string[] args, ServiceProvider services)
{
	var name = GetValue(args, "--name");
	var email = GetValue(args, "--email");
	var notes = GetValue(args, "--notes");

	if (string.IsNullOrWhiteSpace(name))
	{
		Console.Error.WriteLine("Missing required option: --name");
		return;
	}

	var contactService = services.GetRequiredService<ContactApplicationService>();
	var created = await contactService.CreateAsync(new CreateContactRequest(name, email, notes));

	Console.WriteLine("Contact created");
	Console.WriteLine($"Name : {created.Name}");
	Console.WriteLine($"Email: {created.Email}");
}

static async Task RunListContactsAsync(ServiceProvider services)
{
	var contactService = services.GetRequiredService<ContactApplicationService>();
	var contacts = await contactService.ListAsync();

	foreach (var contact in contacts)
	{
		Console.WriteLine($"{contact.Name}  {contact.Email}");
	}
}

static async Task RunDeleteContactAsync(string[] args, ServiceProvider services)
{
	var name = GetValue(args, "--name");
	if (string.IsNullOrWhiteSpace(name))
	{
		Console.Error.WriteLine("Missing required option: --name");
		return;
	}

	var contactService = services.GetRequiredService<ContactApplicationService>();
	var deleted = await contactService.DeleteAsync(name);
	Console.WriteLine(deleted ? "Contact deleted" : "Contact not found");
}

static async Task RunAuditListAsync(ServiceProvider services)
{
	var auditRepository = services.GetRequiredService<DarkStar.Application.Abstractions.IAuditRepository>();
	var records = await auditRepository.ReadAllAsync();

	foreach (var record in records)
	{
		Console.WriteLine($"{record.Timestamp:O}  {record.Operation}  {record.Subject}  {record.Metadata}");
	}
}

static async Task RunAuditVerifyAsync(ServiceProvider services)
{
	var auditService = services.GetRequiredService<AuditApplicationService>();
	var valid = await auditService.VerifyIntegrityAsync();
	Console.WriteLine($"Audit integrity valid: {valid}");
}

static async Task RunBackupCreateAsync(string[] args, ServiceProvider services)
{
	var output = GetValue(args, "--output");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";
	var signingSecret = GetValue(args, "--signing-secret");
	var signingPrivateKeyPem = GetValue(args, "--signing-private-key-pem");
	var signatureMode = GetValue(args, "--signature-mode");

	if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --output and --passphrase");
		return;
	}

	var backupService = services.GetRequiredService<BackupApplicationService>();
	var result = await backupService.CreateBackupAsync(
		new CreateBackupRequest(output, passphrase, algorithm, signingSecret, signingPrivateKeyPem, signatureMode));

	Console.WriteLine("Backup created");
	Console.WriteLine($"Path      : {result.BackupPath}");
	Console.WriteLine($"Hash      : {result.IntegrityHash}");
	Console.WriteLine($"Signature : {result.ManifestSignature ?? "(none)"}");
	Console.WriteLine($"Sig Algo  : {result.ManifestSignatureAlgorithm ?? "(none)"}");
	Console.WriteLine($"Bytes     : {result.ByteCount}");
	Console.WriteLine($"Created At: {result.CreatedAt:O}");
}

static async Task RunBackupVerifyAsync(string[] args, ServiceProvider services)
{
	var input = GetValue(args, "--input");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";
	var signingSecret = GetValue(args, "--signing-secret");
	var signingPublicKeyPem = GetValue(args, "--signing-public-key-pem");
	var signatureMode = GetValue(args, "--signature-mode");

	if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --input and --passphrase");
		return;
	}

	var backupService = services.GetRequiredService<BackupApplicationService>();
	var result = await backupService.VerifyBackupAsync(
		new VerifyBackupRequest(input, passphrase, algorithm, signingSecret, signingPublicKeyPem, signatureMode));

	Console.WriteLine($"Backup valid : {result.IsValid}");
	Console.WriteLine($"Integrity    : {result.IntegrityHash}");
	Console.WriteLine($"Signature    : present={result.IsSignaturePresent} valid={result.IsSignatureValid}");
	Console.WriteLine($"Sig Algo     : {result.ManifestSignatureAlgorithm ?? "(none)"}");
	Console.WriteLine($"Counts       : keys={result.KeyCount} contacts={result.ContactCount} audit={result.AuditCount}");
}

static async Task RunBackupRestoreAsync(string[] args, ServiceProvider services)
{
	var input = GetValue(args, "--input");
	var passphrase = GetValue(args, "--passphrase");
	var algorithm = GetValue(args, "--algorithm") ?? "aes256gcm";
	var signingSecret = GetValue(args, "--signing-secret");
	var signingPublicKeyPem = GetValue(args, "--signing-public-key-pem");
	var signatureMode = GetValue(args, "--signature-mode");

	if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(passphrase))
	{
		Console.Error.WriteLine("Missing required options: --input and --passphrase");
		return;
	}

	var backupService = services.GetRequiredService<BackupApplicationService>();
	var result = await backupService.RestoreBackupAsync(
		new RestoreBackupRequest(input, passphrase, algorithm, signingSecret, signingPublicKeyPem, signatureMode));

	Console.WriteLine("Backup restored");
	Console.WriteLine($"Integrity: {result.IntegrityHash}");
	Console.WriteLine($"Signature verified: {result.SignatureVerified}");
	Console.WriteLine($"Sig Algo: {result.ManifestSignatureAlgorithm ?? "(none)"}");
	Console.WriteLine($"Counts   : keys={result.KeyCount} contacts={result.ContactCount} audit={result.AuditCount}");
}

static string? GetValue(IReadOnlyList<string> args, string option)
{
	var idx = -1;
	for (var i = 0; i < args.Count; i++)
	{
		if (string.Equals(args[i], option, StringComparison.Ordinal))
		{
			idx = i;
			break;
		}
	}

	if (idx < 0 || idx + 1 >= args.Count)
	{
		return null;
	}

	return args[idx + 1];
}

static void PrintUsage()
{
	Console.WriteLine("DarkStar CLI (.NET)");
	Console.WriteLine();
	Console.WriteLine("Commands:");
	Console.WriteLine("  encrypt-text --text <value> --passphrase <value> [--algorithm aes256gcm]");
	Console.WriteLine("  encrypt-file --input <path> --passphrase <value> [--output path] [--algorithm aes256gcm]");
	Console.WriteLine("  decrypt-text --ciphertext <base64> --passphrase <value> [--algorithm aes256gcm]");
	Console.WriteLine("  decrypt-file --input <path> --passphrase <value> [--output path] [--algorithm aes256gcm]");
	Console.WriteLine("  sign-text --message <value> --secret <value>");
	Console.WriteLine("  verify-text --message <value> --secret <value> --signature <base64>");
	Console.WriteLine("  workspace-summary");
	Console.WriteLine("  create-key --label <value> [--algorithm aes256gcm]");
	Console.WriteLine("  list-keys");
	Console.WriteLine("  delete-key --fingerprint <value>");
	Console.WriteLine("  add-contact --name <value> [--email value] [--notes value]");
	Console.WriteLine("  list-contacts");
	Console.WriteLine("  delete-contact --name <value>");
	Console.WriteLine("  audit-list");
	Console.WriteLine("  audit-verify");
	Console.WriteLine("  backup-create --output <path> --passphrase <value> [--algorithm aes256gcm] [--signing-secret value] [--signing-private-key-pem value] [--signature-mode hmac|rsa]");
	Console.WriteLine("  backup-verify --input <path> --passphrase <value> [--algorithm aes256gcm] [--signing-secret value] [--signing-public-key-pem value] [--signature-mode hmac|rsa]");
	Console.WriteLine("  backup-restore --input <path> --passphrase <value> [--algorithm aes256gcm] [--signing-secret value] [--signing-public-key-pem value] [--signature-mode hmac|rsa]");
}
