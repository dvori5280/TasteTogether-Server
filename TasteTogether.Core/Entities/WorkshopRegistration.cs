namespace TasteTogether.Core.Entities;

public class WorkshopRegistration
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int WorkshopId { get; set; }

    public Workshop Workshop { get; set; } = null!;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}