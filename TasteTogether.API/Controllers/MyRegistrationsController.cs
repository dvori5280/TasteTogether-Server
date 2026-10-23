using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteTogether.API.Extensions;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.API.Controllers;

[ApiController]
[Authorize]
[ProducesResponseType(401)]
[Route("api/registrations")]
public class MyRegistrationsController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public MyRegistrationsController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<RegistrationResponse>>> GetMineAsync(
        CancellationToken cancellationToken)
    {
        return Ok(await _registrationService.GetByUserAsync(User.GetUserId(), cancellationToken));
    }
}