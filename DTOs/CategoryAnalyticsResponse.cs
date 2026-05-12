using StarterApp.Enums;

namespace StarterApp.DTOs;

public class CategoryAnalyticsResponse
{
    public string Category { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageAmount { get; set; }
}
