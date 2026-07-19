using DarkStar.Api.Contracts;
using DarkStar.Application.Abstractions;
using DarkStar.Application.Services;
using DarkStar.Domain.Audit;
using Microsoft.AspNetCore.Mvc;

namespace DarkStar.Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController(
    IAuditRepository auditRepository,
    AuditApplicationService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditRecord>>> List(CancellationToken cancellationToken)
        => Ok(await auditRepository.ReadAllAsync(cancellationToken));

    [HttpGet("verify")]
    public async Task<ActionResult<AuditVerifyResponseDto>> Verify(CancellationToken cancellationToken)
    {
        var valid = await auditService.VerifyIntegrityAsync(cancellationToken);
        return Ok(new AuditVerifyResponseDto(valid));
    }
}
