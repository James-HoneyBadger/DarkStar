using DarkStar.Api.Contracts;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DarkStar.Api.Controllers;

[ApiController]
[Route("api/backup")]
public sealed class BackupController(BackupApplicationService backupService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<ActionResult<CreateBackupResponseDto>> Create(
        [FromBody] CreateBackupRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await backupService.CreateBackupAsync(
            new CreateBackupRequest(
                request.OutputPath,
                request.Passphrase,
                request.Algorithm,
                request.SigningSecret,
                request.SigningPrivateKeyPem,
                request.SignatureMode),
            cancellationToken);

        return Ok(new CreateBackupResponseDto(
            result.BackupPath,
            result.IntegrityHash,
            result.ManifestSignature,
            result.ManifestSignatureAlgorithm,
            result.ByteCount,
            result.CreatedAt));
    }

    [HttpPost("verify")]
    public async Task<ActionResult<VerifyBackupResponseDto>> Verify(
        [FromBody] VerifyBackupRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await backupService.VerifyBackupAsync(
            new VerifyBackupRequest(
                request.BackupPath,
                request.Passphrase,
                request.Algorithm,
                request.SigningSecret,
                request.SigningPublicKeyPem,
                request.SignatureMode),
            cancellationToken);

        return Ok(new VerifyBackupResponseDto(
            result.IsValid,
            result.IntegrityHash,
            result.IsSignaturePresent,
            result.IsSignatureValid,
            result.ManifestSignatureAlgorithm,
            result.KeyCount,
            result.ContactCount,
            result.AuditCount));
    }

    [HttpPost("restore")]
    public async Task<ActionResult<RestoreBackupResponseDto>> Restore(
        [FromBody] RestoreBackupRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await backupService.RestoreBackupAsync(
            new RestoreBackupRequest(
                request.BackupPath,
                request.Passphrase,
                request.Algorithm,
                request.SigningSecret,
                request.SigningPublicKeyPem,
                request.SignatureMode),
            cancellationToken);

        return Ok(new RestoreBackupResponseDto(
            result.KeyCount,
            result.ContactCount,
            result.AuditCount,
            result.IntegrityHash,
            result.SignatureVerified,
            result.ManifestSignatureAlgorithm));
    }
}
