using StarterApp.DTOs;

namespace StarterApp.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterUserAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RefreshResponse> RefreshTokenAsync(RefreshRequest request);
    Task<bool> UserExistsAsync(string username, string email);
}
