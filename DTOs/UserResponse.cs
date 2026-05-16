namespace StarterApp.DTOs;

public class UserResponse
{
    public Guid Id {get; set;}
    public string Email {get; set;} = string.Empty;
    public string? Username {get; set;}
    public string? FirstName {get; set;}
    public string? LastName {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
    public string Role {get; set;} = string.Empty;
}
