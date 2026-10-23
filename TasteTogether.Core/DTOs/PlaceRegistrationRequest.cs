using System.ComponentModel.DataAnnotations;

namespace TasteTogether.Core.DTOs;

public class PlaceRegistrationRequest
{
    // ה-Version של הסדנה כפי שהלקוחה ראתה אותו ב-WorkshopResponse.
    // Guid? עם Required: שדה חסר נדחה ב-400, ולא נהפך לאפסים שנותנים 409 מבלבל
    [Required]
    public Guid? Version { get; set; }
}