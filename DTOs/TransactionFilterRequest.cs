using System.ComponentModel.DataAnnotations;
using StarterApp.Enums;

namespace StarterApp.DTOs;

public class TransactionFilterRequest
{
    public TransactionType? Type { get; set; }
    
    public string? Category { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
}
