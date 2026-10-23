using Microsoft.AspNetCore.Mvc;
using TasteTogether.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace TasteTogether.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[ProducesResponseType(401)]
[ProducesResponseType(403)]
[Route("api/workshops/{workshopId}/recipes")]
public class RecipeWorkshopLinksController : ControllerBase
{
    private readonly IWorkshopService _workshopService;

    public RecipeWorkshopLinksController(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [HttpPost("{recipeId}")]
    public async Task<IActionResult> LinkRecipeAsync(
        [FromRoute] int workshopId, [FromRoute] int recipeId, CancellationToken cancellationToken)
    {
        await _workshopService.LinkRecipeAsync(workshopId, recipeId, cancellationToken);
        return NoContent();
    }

    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [HttpDelete("{recipeId}")]
    public async Task<IActionResult> UnlinkRecipeAsync(
        [FromRoute] int workshopId, [FromRoute] int recipeId, CancellationToken cancellationToken)
    {
        await _workshopService.UnlinkRecipeAsync(workshopId, recipeId, cancellationToken);
        return NoContent();
    }

    // TODO (שלב L13): הצגת המתכונים של סדנה והסדנאות של מתכון. תלוי בהחלטה על Service ו-DTOs למתכונים.
}