using StarterApp.Enums;

namespace StarterApp.DTOs;

public class TransactionFilterRequest
{
    public TransactionType? Type { get; set; }
    public string? Category { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
