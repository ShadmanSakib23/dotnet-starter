using Microsoft.EntityFrameworkCore;
using StarterApp.Data;
using StarterApp.DTOs;
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

    public async Task<PagedResult<User>> GetAllAsync(int page, int pageSize)
    {
        var totalCount = await _dbSet.CountAsync();
        var users = await _dbSet
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = users,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
