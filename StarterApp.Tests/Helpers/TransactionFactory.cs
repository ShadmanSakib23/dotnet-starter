using StarterApp.Enums;
using StarterApp.Models;

namespace StarterApp.Tests.Helpers;

public static class TransactionFactory
{
    public static Transaction Create(
        Guid? id = null,
        Guid? userId = null,
        TransactionType type = TransactionType.Income,
        string category = "Salary",
        decimal amount = 1000.00m,
        DateTime? processedAt = null)
    {
        return new Transaction
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Type = type,
            Category = category,
            Amount = amount,
            ProcessedAt = processedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
