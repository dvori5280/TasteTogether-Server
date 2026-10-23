using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Interfaces;


namespace TasteTogether.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[ProducesResponseType(401)]
[ProducesResponseType(403)]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetAllAsync(cancellationToken));
    }

    [ProducesResponseType(404)]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();

        return Ok(user);
    }
}