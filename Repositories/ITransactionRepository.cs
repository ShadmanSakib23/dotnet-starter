using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Models;

namespace StarterApp.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<PagedResult<Transaction>> GetUserTransactionsAsync(Guid userId, TransactionFilterRequest filter);
    
    // Analytics methods
    Task<List<CategoryAnalyticsResponse>> GetCategoryAnalyticsAsync(
        Guid userId, 
        DateTime? startDate, 
        DateTime? endDate, 
        TransactionType? type);
    
    Task<List<MonthlyAnalyticsResponse>> GetMonthlyAnalyticsAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate);
    
    Task<AnalyticsSummaryResponse> GetSummaryAnalyticsAsync(
        Guid userId, 
        DateTime? startDate, 
        DateTime? endDate);
}
