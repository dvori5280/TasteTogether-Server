using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> LoginAsync(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        if (response == null)
            return Unauthorized();

        return Ok(response);
    }

    [AllowAnonymous]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> RegisterAsync(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);

        return CreatedAtAction("GetById", "Users", new { id = response.User.Id }, response);
    }
}