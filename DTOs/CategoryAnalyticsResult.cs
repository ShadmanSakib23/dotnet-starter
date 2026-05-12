namespace StarterApp.DTOs;

public class CategoryAnalyticsResult
{
    public List<CategoryAnalyticsResponse> IncomeByCategory { get; set; } = new();
    public List<CategoryAnalyticsResponse> ExpenseByCategory { get; set; } = new();
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
    public AnalyticsDateRange DateRange { get; set; } = new();
}
