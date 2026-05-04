using System.ComponentModel.DataAnnotations;

namespace StarterApp.DTOs;

public class UpdateUserRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

}