using System.ComponentModel.DataAnnotations;

namespace StarterApp.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}
