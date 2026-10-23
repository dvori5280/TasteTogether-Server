using Microsoft.AspNetCore.Mvc;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using TasteTogether.API.Extensions;

namespace TasteTogether.API.Controllers;

[ApiController]
[Route("api/workshops/{workshopId}/registrations")]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationsController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<RegistrationResponse>>> GetByWorkshopAsync(
        [FromRoute] int workshopId, CancellationToken cancellationToken)
    {
        return Ok(await _registrationService.GetByWorkshopAsync(workshopId, cancellationToken));
    }

    [Authorize]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [HttpPost]
    public async Task<ActionResult<RegistrationResponse>> RegisterAsync(
    [FromRoute] int workshopId,
    [FromBody] PlaceRegistrationRequest request,
    CancellationToken cancellationToken)
    {
        var registration = await _registrationService.RegisterAsync(
            workshopId, User.GetUserId(), request, cancellationToken);

        return CreatedAtAction("GetById", "Workshops", new { id = workshopId }, registration);
    }

    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [HttpDelete]
    public async Task<IActionResult> CancelAsync(
    [FromRoute] int workshopId, CancellationToken cancellationToken)
    {
        await _registrationService.CancelAsync(workshopId, User.GetUserId(), cancellationToken);

        return NoContent();
    }
}