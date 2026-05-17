using StarterApp.DTOs;
using StarterApp.Enums;

namespace StarterApp.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetUserAsync(Guid Id);
    Task<UserResponse> UpdateUserAsync(Guid Id, UpdateUserRequest request);
    Task<PagedResult<UserResponse>> GetAllUsersAsync(int page, int pageSize);
    Task<UserResponse> GetUserByIdAsync(Guid requesterId, Guid targetId);
    Task DeleteUserAsync(Guid requesterId, Guid targetId);
    Task<UserResponse> AssignRoleAsync(Guid requesterId, Guid targetId, UserRole newRole);
}