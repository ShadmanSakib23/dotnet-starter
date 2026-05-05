using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Models;

namespace StarterApp.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<PagedResult<Transaction>> GetUserTransactionsAsync(Guid userId, TransactionFilterRequest filter);
}
