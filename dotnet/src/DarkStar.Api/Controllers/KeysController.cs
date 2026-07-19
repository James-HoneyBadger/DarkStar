using DarkStar.Api.Contracts;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using DarkStar.Domain.Keys;
using Microsoft.AspNetCore.Mvc;

namespace DarkStar.Api.Controllers;

[ApiController]
[Route("api/keys")]
public sealed class KeysController(KeyApplicationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<KeyRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<KeyRecord>>> List(CancellationToken cancellationToken)
    {
        var keys = await service.ListAsync(cancellationToken);
        return Ok(keys);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateKeyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateKeyResult>> Create(
        [FromBody] CreateKeyApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await service.CreateAsync(new CreateKeyRequest(request.Algorithm, request.Label), cancellationToken);
            return Ok(key);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{fingerprint}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string fingerprint, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(fingerprint, cancellationToken);
        return deleted ? Ok(new { deleted = true }) : NotFound(new { deleted = false });
    }
}
