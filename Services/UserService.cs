using StarterApp.Interfaces;
using StarterApp.Repositories;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Exceptions;

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
            UpdatedAt = user.UpdatedAt,
            Role = user.Role.ToString()
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
            UpdatedAt = updatedUser.UpdatedAt,
            Role = updatedUser.Role.ToString()
        };
    }

    public async Task<PagedResult<UserResponse>> GetAllUsersAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var pagedUsers = await _userRepository.GetAllAsync(page, pageSize);
        
        var userResponses = pagedUsers.Items.Select(user => new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Role = user.Role.ToString()
        });

        return new PagedResult<UserResponse>
        {
            Items = userResponses,
            Page = pagedUsers.Page,
            PageSize = pagedUsers.PageSize,
            TotalCount = pagedUsers.TotalCount,
            TotalPages = pagedUsers.TotalPages
        };
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid requesterId, Guid targetId)
    {
        var user = await _userRepository.GetByIdAsync(targetId);
        
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {targetId} not found.");
        }

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Role = user.Role.ToString()
        };
    }

    public async Task DeleteUserAsync(Guid requesterId, Guid targetId)
    {
        if (requesterId == targetId)
        {
            throw new UnauthorizedAccessException("Cannot delete your own account.");
        }

        var user = await _userRepository.GetByIdAsync(targetId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {targetId} not found.");
        }

        await _userRepository.DeleteAsync(targetId);
    }

    public async Task<UserResponse> AssignRoleAsync(Guid requesterId, Guid targetId, UserRole newRole)
    {
        if (newRole == UserRole.SuperAdmin)
        {
            throw new UnauthorizedAccessException("Cannot assign SuperAdmin role via API.");
        }

        var user = await _userRepository.GetByIdAsync(targetId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {targetId} not found.");
        }

        if ((int)newRole <= (int)user.Role)
        {
            throw new BadRequestException($"Cannot assign role {newRole}. Role can only be upgraded, not demoted.");
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Role = user.Role.ToString()
        };
    }
    
}