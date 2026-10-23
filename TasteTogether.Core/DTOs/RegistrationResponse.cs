namespace TasteTogether.Core.DTOs;

public class RegistrationResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int WorkshopId { get; set; }
    public DateTime RegisteredAt { get; set; }

    // לתצוגת "ההרשמות שלי"
    public string WorkshopTitle { get; set; } = string.Empty;

    // כדי שנרשמת תראה שהסדנה בוטלה
    public string WorkshopStatus { get; set; } = string.Empty;

    // לתצוגת רשימת הנרשמים ל-Admin
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
}