using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteTogether.API.Extensions;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Interfaces;
using TasteTogether.Core.Enums;

namespace TasteTogether.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [AllowAnonymous]
    [ProducesResponseType(400)]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<RecipeResponse>>> GetAllAsync(
    [FromQuery] string? search = null,
    [FromQuery] RecipeSortOrder sort = RecipeSortOrder.Newest,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
    {
        return Ok(await _recipeService.GetAllAsync(search, sort, page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [ProducesResponseType(404)]
    [HttpGet("{id}")]
    public async Task<ActionResult<RecipeResponse>> GetByIdAsync(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        var recipe = await _recipeService.GetByIdAsync(id, cancellationToken);
        if (recipe == null)
            return NotFound();

        return Ok(recipe);
    }

    [Authorize(Roles = "Admin")]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [HttpPost]
    public async Task<ActionResult<RecipeResponse>> CreateAsync(
        [FromBody] CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeService.CreateAsync(request, User.GetUserId(), cancellationToken);

        return CreatedAtAction("GetById", new { id = recipe.Id }, recipe);
    }

    [Authorize(Roles = "Admin")]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [HttpPut("{id}")]
    public async Task<ActionResult<RecipeResponse>> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _recipeService.UpdateAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id, CancellationToken cancellationToken)
    {
        await _recipeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}