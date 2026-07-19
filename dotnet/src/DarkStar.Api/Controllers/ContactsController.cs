using DarkStar.Api.Contracts;
using DarkStar.Application.Models;
using DarkStar.Application.Services;
using DarkStar.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace DarkStar.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public sealed class ContactsController(ContactApplicationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ContactRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ContactRecord>>> List(CancellationToken cancellationToken)
    {
        var contacts = await service.ListAsync(cancellationToken);
        return Ok(contacts);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContactRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactRecord>> Create(
        [FromBody] CreateContactApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var contact = await service.CreateAsync(
                new CreateContactRequest(request.Name, request.Email, request.Notes),
                cancellationToken
            );
            return Ok(contact);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(name, cancellationToken);
        return deleted ? Ok(new { deleted = true }) : NotFound(new { deleted = false });
    }
}
