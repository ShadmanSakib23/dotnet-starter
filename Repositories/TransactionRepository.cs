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

    public async Task<List<CategoryAnalyticsResponse>> GetCategoryAnalyticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        TransactionType? type)
    {
        var query = _dbSet.Where(t => t.UserId == userId);

        // Apply date filters
        if (startDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAt <= endDate.Value);
        }

        // Apply type filter if specified
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        // Group by category and type, calculate aggregates
        var result = await query
            .GroupBy(t => new { t.Category, t.Type })
            .Select(g => new CategoryAnalyticsResponse
            {
                Category = g.Key.Category,
                Type = g.Key.Type,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageAmount = g.Average(t => t.Amount),
                Percentage = 0 // Will be calculated in service layer
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync();

        return result;
    }

    public async Task<List<MonthlyAnalyticsResponse>> GetMonthlyAnalyticsAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate)
    {
        var transactions = await _dbSet
            .Where(t => t.UserId == userId)
            .Where(t => t.ProcessedAt >= startDate && t.ProcessedAt <= endDate)
            .Select(t => new { t.ProcessedAt, t.Type, t.Amount })
            .ToListAsync();

        // Group by year and month
        var result = transactions
            .GroupBy(t => new { t.ProcessedAt.Year, t.ProcessedAt.Month })
            .Select(g => new MonthlyAnalyticsResponse
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                TotalIncome = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                TotalExpenses = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                NetAmount = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) -
                           g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return result;
    }

    public async Task<AnalyticsSummaryResponse> GetSummaryAnalyticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = _dbSet.Where(t => t.UserId == userId);

        // Apply date filters
        if (startDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAt <= endDate.Value);
        }

        var transactions = await query
            .Select(t => new { t.Type, t.Amount, t.Category })
            .ToListAsync();

        var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income).ToList();
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense).ToList();

        var topIncomeCategory = incomeTransactions
            .GroupBy(t => t.Category)
            .OrderByDescending(g => g.Sum(t => t.Amount))
            .Select(g => g.Key)
            .FirstOrDefault();

        var topExpenseCategory = expenseTransactions
            .GroupBy(t => t.Category)
            .OrderByDescending(g => g.Sum(t => t.Amount))
            .Select(g => g.Key)
            .FirstOrDefault();

        return new AnalyticsSummaryResponse
        {
            TotalIncome = incomeTransactions.Sum(t => t.Amount),
            TotalExpenses = expenseTransactions.Sum(t => t.Amount),
            NetAmount = incomeTransactions.Sum(t => t.Amount) - expenseTransactions.Sum(t => t.Amount),
            TotalTransactions = transactions.Count,
            IncomeTransactionCount = incomeTransactions.Count,
            ExpenseTransactionCount = expenseTransactions.Count,
            TopIncomeCategory = topIncomeCategory,
            TopExpenseCategory = topExpenseCategory,
            DateRange = new AnalyticsDateRange
            {
                StartDate = startDate ?? DateTime.MinValue,
                EndDate = endDate ?? DateTime.MaxValue
            }
        };
    }
}