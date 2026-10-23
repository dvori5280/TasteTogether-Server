using Microsoft.AspNetCore.Mvc;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using TasteTogether.API.Extensions;


namespace TasteTogether.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;

    public WorkshopsController(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    // פתוח לכולן. אם נשלח Token של אדמינית, מוחזרות כל הסדנאות. הפרמטר status פעיל רק עבורה.
    // recipeId מסנן לסדנאות שהמתכון מקושר אליהן
    [AllowAnonymous]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkshopResponse>>> GetAllAsync(
        [FromQuery] WorkshopStatus? status = null,
        [FromQuery] int? recipeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _workshopService.GetAllAsync(
            status, recipeId, User.IsInRole("Admin"), page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [ProducesResponseType(404)]
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkshopResponse>> GetByIdAsync(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.GetByIdAsync(id, User.IsInRole("Admin"), cancellationToken);
        if (workshop == null)
            return NotFound();

        return Ok(workshop);
    }

    [AllowAnonymous]
    [ProducesResponseType(404)]
    [HttpGet("{id}/recipes")]
    public async Task<ActionResult<List<RecipeResponse>>> GetRecipesAsync(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await _workshopService.GetRecipesAsync(id, User.IsInRole("Admin"), cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpPost]
    public async Task<ActionResult<WorkshopResponse>> CreateAsync(
    [FromBody] CreateWorkshopRequest request, CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.CreateAsync(request, User.GetUserId(), cancellationToken);

        return CreatedAtAction("GetById", new { id = workshop.Id }, workshop);
    }

    [Authorize(Roles = "Admin")]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [HttpPut("{id}/status")]
    public async Task<ActionResult<WorkshopResponse>> UpdateStatusAsync(
        [FromRoute] int id,
        [FromBody] UpdateWorkshopStatusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _workshopService.UpdateStatusAsync(id, request, cancellationToken));
    }

}