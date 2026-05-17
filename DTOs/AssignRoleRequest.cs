using System.ComponentModel.DataAnnotations;
using StarterApp.Enums;

namespace StarterApp.DTOs;

public class AssignRoleRequest
{
    [Required]
    public UserRole Role { get; set; }
}
