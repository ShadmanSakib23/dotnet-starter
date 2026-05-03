using Microsoft.EntityFrameworkCore;
using StarterApp.Data;
using StarterApp.Models;

namespace StarterApp.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> UserExistsAsync(string username, string email)
    {
        return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower() ||
                                          (u.Username != null && u.Username.ToLower() == username.ToLower()));
    }
}
