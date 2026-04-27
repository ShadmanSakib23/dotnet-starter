using System.ComponentModel.DataAnnotations;

namespace StarterApp.DTOs;

public class RegisterRequest
{
    [MaxLength(100)]
    public string? FirstName {get; set;}
    
    [MaxLength(100)]
    public string? LastName {get; set;}
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage ="Email is invalid")]
    [MaxLength(255)]
    public string Email {get; set;} = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100)]
    public string Password {get; set;} = string.Empty;
}