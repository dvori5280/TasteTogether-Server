using TasteTogether.Core.Enums;

namespace TasteTogether.Core.Entities;

public class Workshop
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public int Capacity { get; set; }

    public WorkshopStatus Status { get; set; } = WorkshopStatus.Draft;

    public Guid Version { get; set; } = Guid.NewGuid();

    public int HostId { get; set; }

    public User Host { get; set; } = null!;

    public List<WorkshopRegistration> Registrations { get; set; } = new();

    public List<Recipe> Recipes { get; set; } = new();
}