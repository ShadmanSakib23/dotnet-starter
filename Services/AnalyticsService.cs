using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Interfaces;
using StarterApp.Repositories;

namespace StarterApp.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        ITransactionRepository transactionRepository,
        ILogger<AnalyticsService> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<CategoryAnalyticsResult> GetCategoryAnalyticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            // Set default date range if not provided (last 30 days)
            var end = endDate ?? DateTime.UtcNow;
            var start = startDate ?? end.AddDays(-30);

            // Get all category analytics
            var allCategories = await _transactionRepository.GetCategoryAnalyticsAsync(
                userId, start, end, null);

            // Separate by type
            var incomeCategories = allCategories
                .Where(c => c.Type == TransactionType.Income)
                .ToList();

            var expenseCategories = allCategories
                .Where(c => c.Type == TransactionType.Expense)
                .ToList();

            // Calculate totals
            var totalIncome = incomeCategories.Sum(c => c.TotalAmount);
            var totalExpenses = expenseCategories.Sum(c => c.TotalAmount);

            // Calculate percentages for income categories
            foreach (var category in incomeCategories)
            {
                category.Percentage = totalIncome > 0
                    ? Math.Round((category.TotalAmount / totalIncome) * 100, 2)
                    : 0;
            }

            // Calculate percentages for expense categories
            foreach (var category in expenseCategories)
            {
                category.Percentage = totalExpenses > 0
                    ? Math.Round((category.TotalAmount / totalExpenses) * 100, 2)
                    : 0;
            }

            _logger.LogInformation(
                "Category analytics generated for user {UserId}: {IncomeCategories} income categories, {ExpenseCategories} expense categories",
                userId, incomeCategories.Count, expenseCategories.Count);

            return new CategoryAnalyticsResult
            {
                IncomeByCategory = incomeCategories,
                ExpenseByCategory = expenseCategories,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetAmount = totalIncome - totalExpenses,
                DateRange = new AnalyticsDateRange
                {
                    StartDate = start,
                    EndDate = end
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category analytics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<MonthlyAnalyticsResponse>> GetMonthlyAnalyticsAsync(
        Guid userId,
        int months = 6)
    {
        try
        {
            // Validate months parameter
            if (months < 1) months = 1;
            if (months > 24) months = 24;

            // Calculate date range
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months + 1);
            startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc); // Start of month (UTC)

            var result = await _transactionRepository.GetMonthlyAnalyticsAsync(
                userId, startDate, endDate);

            _logger.LogInformation(
                "Monthly analytics generated for user {UserId}: {Months} months of data",
                userId, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly analytics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<AnalyticsSummaryResponse> GetSummaryAnalyticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            // Set default date range if not provided (last 30 days)
            var end = endDate ?? DateTime.UtcNow;
            var start = startDate ?? end.AddDays(-30);

            var result = await _transactionRepository.GetSummaryAnalyticsAsync(
                userId, start, end);

            // Update date range with actual values
            result.DateRange = new AnalyticsDateRange
            {
                StartDate = start,
                EndDate = end
            };

            _logger.LogInformation(
                "Summary analytics generated for user {UserId}: Income={Income}, Expenses={Expenses}, Net={Net}",
                userId, result.TotalIncome, result.TotalExpenses, result.NetAmount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating summary analytics for user {UserId}", userId);
            throw;
        }
    }
}
