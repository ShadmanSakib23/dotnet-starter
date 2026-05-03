using StarterApp.DTOs;

namespace StarterApp.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetUserAsync(Guid Id);
}