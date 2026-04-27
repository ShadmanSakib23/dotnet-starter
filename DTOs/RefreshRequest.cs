using System.ComponentModel.DataAnnotations;

namespace StarterApp.DTOs;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
