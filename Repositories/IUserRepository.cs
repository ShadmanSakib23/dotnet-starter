using StarterApp.DTOs;
using StarterApp.Models;

namespace StarterApp.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UserExistsAsync(string username, string email);
    Task<PagedResult<User>> GetAllAsync(int page, int pageSize);
}
