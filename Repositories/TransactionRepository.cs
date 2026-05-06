using Microsoft.EntityFrameworkCore;
using StarterApp.Data;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Models;

namespace StarterApp.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Transaction>> GetUserTransactionsAsync(Guid userId, TransactionFilterRequest filter)
    {
        var query = _dbSet.Where(t => t.UserId == userId);

        // Apply filters
        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(t => t.Category == filter.Category);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAt <= filter.EndDate.Value);
        }
        
        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(t => t.ProcessedAt)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Transaction>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }
}
