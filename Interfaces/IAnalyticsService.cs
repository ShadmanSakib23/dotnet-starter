using StarterApp.DTOs;

namespace StarterApp.Interfaces;

public interface IAnalyticsService
{
    Task<CategoryAnalyticsResult> GetCategoryAnalyticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate);

    Task<List<MonthlyAnalyticsResponse>> GetMonthlyAnalyticsAsync(
        Guid userId,
        int months = 6);

    Task<AnalyticsSummaryResponse> GetSummaryAnalyticsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate);
}
