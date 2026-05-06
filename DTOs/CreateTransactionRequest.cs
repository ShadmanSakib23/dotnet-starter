using System.ComponentModel.DataAnnotations;
using StarterApp.Enums;

namespace StarterApp.DTOs;

public class CreateTransactionRequest
{
    [Required(ErrorMessage = "Transaction type is required")]
    public TransactionType? Type { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}
