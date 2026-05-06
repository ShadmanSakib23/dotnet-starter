using StarterApp.DTOs;
using StarterApp.Interfaces;
using StarterApp.Models;
using StarterApp.Repositories;

namespace StarterApp.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ILogger<TransactionService> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<TransactionResponse> CreateTransactionAsync(Guid userId, CreateTransactionRequest request)
    {
        try
        {
            var transaction = new Transaction
            {
                UserId = userId,
                Type = request.Type!.Value,
                Category = request.Category,
                Amount = request.Amount,
                ProcessedAt = DateTime.UtcNow
            };

            var createdTransaction = await _transactionRepository.AddAsync(transaction);

            _logger.LogInformation(
                "Transaction created for user {UserId}: {Type} of {Amount} in category {Category}",
                userId, createdTransaction.Type, createdTransaction.Amount, createdTransaction.Category);

            return MapToResponse(createdTransaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
            throw;
        }
    }

    public async Task<TransactionResponse> GetTransactionAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId);

        if (transaction == null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} not found");
        }

        if (transaction.UserId != userId)
        {
            throw new UnauthorizedAccessException("You don't have access to this transaction");
        }

        return MapToResponse(transaction);
    }

    public async Task<PagedResult<TransactionResponse>> GetUserTransactionsAsync(
        Guid userId, 
        TransactionFilterRequest filter)
    {
        try
        {
            var pagedTransactions = await _transactionRepository.GetUserTransactionsAsync(userId, filter);

            return new PagedResult<TransactionResponse>
            {
                Items = pagedTransactions.Items.Select(MapToResponse),
                Page = pagedTransactions.Page,
                PageSize = pagedTransactions.PageSize,
                TotalCount = pagedTransactions.TotalCount,
                TotalPages = pagedTransactions.TotalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteTransactionAsync(Guid userId, Guid transactionId)
    {
        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);

            if (transaction == null)
            {
                throw new KeyNotFoundException($"Transaction with ID {transactionId} not found");
            }

            if (transaction.UserId != userId)
            {
                throw new UnauthorizedAccessException("You don't have access to this transaction");
            }

            await _transactionRepository.DeleteAsync(transactionId);

            _logger.LogInformation("Transaction {TransactionId} deleted by user {UserId}", transactionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {TransactionId} for user {UserId}", 
                transactionId, userId);
            throw;
        }
    }

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            Type = transaction.Type,
            Category = transaction.Category,
            Amount = transaction.Amount,
            ProcessedAt = transaction.ProcessedAt,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
