using DarkStar.Application.Services;
using DarkStar.Domain.Workspace;
using Microsoft.AspNetCore.Mvc;

namespace DarkStar.Api.Controllers;

[ApiController]
[Route("api/workspace")]
public sealed class WorkspaceController(WorkspaceApplicationService service) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(WorkspaceSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceSummary>> Summary(CancellationToken cancellationToken)
    {
        var summary = await service.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
