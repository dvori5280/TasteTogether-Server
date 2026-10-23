using System.ComponentModel.DataAnnotations;

namespace TasteTogether.Core.DTOs;

public class UpdateRecipeRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MinLength(1)]
    public List<string> IngredientsText { get; set; } = new();

    [MinLength(1)]
    public List<string> InstructionsText { get; set; } = new();
}