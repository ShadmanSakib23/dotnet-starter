using StarterApp.Enums;
using StarterApp.Models;

namespace StarterApp.Tests.Helpers;

public static class UserFactory
{
    public static User Create(
        Guid? id = null,
        string email = "test@example.com",
        string passwordHash = "hashed_password",
        string? firstName = "John",
        string? lastName = "Doe",
        string? username = "johndoe",
        UserRole role = UserRole.User)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Username = username,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
