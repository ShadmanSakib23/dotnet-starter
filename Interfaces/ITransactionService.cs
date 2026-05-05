using StarterApp.DTOs;

namespace StarterApp.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponse> CreateTransactionAsync(Guid userId, CreateTransactionRequest request);
    Task<TransactionResponse> GetTransactionAsync(Guid userId, Guid transactionId);
    Task<PagedResult<TransactionResponse>> GetUserTransactionsAsync(Guid userId, TransactionFilterRequest filter);
    Task DeleteTransactionAsync(Guid userId, Guid transactionId);
}
