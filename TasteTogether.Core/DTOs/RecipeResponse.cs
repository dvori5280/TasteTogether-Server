namespace TasteTogether.Core.DTOs;

public class RecipeResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> IngredientsText { get; set; } = new();

    public List<string> InstructionsText { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int UserId { get; set; }
}