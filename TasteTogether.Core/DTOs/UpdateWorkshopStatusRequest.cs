using System.ComponentModel.DataAnnotations;

namespace TasteTogether.Core.DTOs;

public class UpdateWorkshopStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}