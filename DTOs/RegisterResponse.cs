using System.ComponentModel.DataAnnotations;

namespace StarterApp.DTOs;

public class RegisterResponse
{
    public Guid Id {get; set;}
    public string Username {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string? FirstName {get; set;}
    public string? LastName {get; set;}
    public DateTime CreatedAt {get; set;}
    public string Role {get; set;} = string.Empty;
}