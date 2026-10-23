namespace TasteTogether.Core.DTOs;

public class WorkshopResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;

    // מחושב מתוך ההרשמות, לא נשמר כשדה בישות
    public int RegisteredCount { get; set; }

    // מחושב ב-Service: האם מועד הסדנה כבר עבר
    public bool IsPast { get; set; }

    // הלקוח מחזיר את הערך הזה בבקשת ההרשמה, לצורך Optimistic Concurrency
    public Guid Version { get; set; }
}