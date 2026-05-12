namespace StarterApp.DTOs;

public class AnalyticsSummaryResponse
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
    public int TotalTransactions { get; set; }
    public int IncomeTransactionCount { get; set; }
    public int ExpenseTransactionCount { get; set; }
    public string? TopIncomeCategory { get; set; }
    public string? TopExpenseCategory { get; set; }
    public AnalyticsDateRange DateRange { get; set; } = new();
}
