using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Models;
using StarterApp.Repositories;
using StarterApp.Services;
using StarterApp.Tests.Helpers;

namespace StarterApp.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<ILogger<TransactionService>> _loggerMock;
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _loggerMock = new Mock<ILogger<TransactionService>>();
        _sut = new TransactionService(_transactionRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithValidData_ReturnsTransactionResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateTransactionRequest
        {
            Type = TransactionType.Income,
            Category = "Salary",
            Amount = 5000m
        };
        _transactionRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        // Act
        var result = await _sut.CreateTransactionAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Category.Should().Be(request.Category);
        result.Amount.Should().Be(request.Amount);
        result.Type.Should().Be(TransactionType.Income);
    }

    [Fact]
    public async Task CreateTransactionAsync_SetsProcessedAtToUtcNow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateTransactionRequest
        {
            Type = TransactionType.Expense,
            Category = "Food",
            Amount = 100m
        };
        var before = DateTime.UtcNow;
        _transactionRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t);

        // Act
        var result = await _sut.CreateTransactionAsync(userId, request);
        var after = DateTime.UtcNow;

        // Assert
        result.ProcessedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task GetTransactionAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        _transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        // Act
        var act = async () => await _sut.GetTransactionAsync(userId, transactionId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetTransactionAsync_WhenUserDoesNotOwnTransaction_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transaction = TransactionFactory.Create(userId: Guid.NewGuid()); // different owner
        _transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);

        // Act
        var act = async () => await _sut.GetTransactionAsync(userId, transaction.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetTransactionAsync_WhenOwned_ReturnsTransactionResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transaction = TransactionFactory.Create(userId: userId);
        _transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);

        // Act
        var result = await _sut.GetTransactionAsync(userId, transaction.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(transaction.Id);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_ReturnsMappedPagedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filter = new TransactionFilterRequest();
        var transactions = new List<Transaction>
        {
            TransactionFactory.Create(userId: userId),
            TransactionFactory.Create(userId: userId)
        };
        var pagedResult = new PagedResult<Transaction>
        {
            Items = transactions,
            Page = 1,
            PageSize = 10,
            TotalCount = 2,
            TotalPages = 1
        };
        _transactionRepositoryMock
            .Setup(r => r.GetUserTransactionsAsync(userId, filter))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetUserTransactionsAsync(userId, filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        _transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        // Act
        var act = async () => await _sut.DeleteTransactionAsync(userId, transactionId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteTransactionAsync_WhenUserDoesNotOwnTransaction_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transaction = TransactionFactory.Create(userId: Guid.NewGuid()); // different owner
        _transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);

        // Act
        var act = async () => await _sut.DeleteTransactionAsync(userId, transaction.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteTransactionAsync_WhenOwned_CallsDeleteAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transaction = TransactionFactory.Create(userId: userId);
        _transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);
        _transactionRepositoryMock
            .Setup(r => r.DeleteAsync(transaction.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteTransactionAsync(userId, transaction.Id);

        // Assert
        _transactionRepositoryMock.Verify(r => r.DeleteAsync(transaction.Id), Times.Once());
    }
}
