using StarterApp.Interfaces;
using StarterApp.Repositories;
using StarterApp.DTOs;

namespace StarterApp.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository userRepository
    )
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse> GetUserAsync(Guid Id)
    {  
        var user = await _userRepository.GetByIdAsync(Id);
        
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {Id} not found.");
        }

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
    
}