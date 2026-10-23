namespace TasteTogether.Core.Entities;

public class Recipe
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> IngredientsText { get; set; } = new();

    public List<string> InstructionsText { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public List<Workshop> Workshops { get; set; } = new();
}