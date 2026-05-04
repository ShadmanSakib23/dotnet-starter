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

    public async Task<UserResponse> UpdateUserAsync(Guid Id, UpdateUserRequest request)
    {
        var updatedUser = await _userRepository.UpdateAsync(Id, user =>
        {
            if (request.FirstName != null) user.FirstName = request.FirstName;
            if (request.LastName != null) user.LastName = request.LastName;
            if (request.Username != null) user.Username = request.Username;

            user.UpdatedAt = DateTime.UtcNow;
        });

        return new UserResponse
        {
            Id = updatedUser.Id,
            Email = updatedUser.Email,
            Username = updatedUser.Username,
            FirstName = updatedUser.FirstName,
            LastName = updatedUser.LastName,
            CreatedAt = updatedUser.CreatedAt,
            UpdatedAt = updatedUser.UpdatedAt
        };
    }
    
}