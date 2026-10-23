using System.ComponentModel.DataAnnotations;

namespace TasteTogether.Core.DTOs;

public class CreateWorkshopRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    // nullable, כי על DateTime רגיל [Required] לא עובד: ערך חסר הופך ל-0001-01-01 והבדיקה עוברת
    [Required]
    public DateTime? StartsAt { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}