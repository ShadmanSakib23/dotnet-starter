using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using StarterApp.Controllers;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Interfaces;

namespace StarterApp.Tests.Controllers;

public class TransactionControllerTests
{
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly Mock<ILogger<TransactionController>> _loggerMock;
    private readonly TransactionController _controller;

    public TransactionControllerTests()
    {
        _transactionServiceMock = new Mock<ITransactionService>();
        _loggerMock = new Mock<ILogger<TransactionController>>();
        _controller = new TransactionController(_transactionServiceMock.Object, _loggerMock.Object);
    }

    private static ControllerContext CreateControllerContext(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        return new ControllerContext { HttpContext = httpContext };
    }

    private static TransactionResponse BuildResponse(Guid userId)
    {
        return new TransactionResponse
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = TransactionType.Income,
            Category = "Salary",
            Amount = 1000m,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ─── CreateTransaction ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTransaction_WithValidData_Returns201Created()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);

        var request = new CreateTransactionRequest
        {
            Type = TransactionType.Income,
            Category = "Salary",
            Amount = 1000m
        };
        var response = BuildResponse(userId);
        _transactionServiceMock.Setup(s => s.CreateTransactionAsync(userId, request))
            .ReturnsAsync(response);

        // Act
        var actionResult = await _controller.CreateTransaction(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(201);
        ((CreatedAtActionResult)result).Value.Should().Be(response);
    }

    [Fact]
    public async Task CreateTransaction_WhenUserIdMissing_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new CreateTransactionRequest
        {
            Type = TransactionType.Income,
            Category = "Salary",
            Amount = 500m
        };

        // Act
        var actionResult = await _controller.CreateTransaction(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }

    // ─── GetTransaction ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransaction_WhenFound_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var transactionId = Guid.NewGuid();
        var response = BuildResponse(userId);
        response.Id = transactionId;
        _transactionServiceMock.Setup(s => s.GetTransactionAsync(userId, transactionId))
            .ReturnsAsync(response);

        // Act
        var actionResult = await _controller.GetTransaction(transactionId);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetTransaction_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var transactionId = Guid.NewGuid();
        _transactionServiceMock.Setup(s => s.GetTransactionAsync(userId, transactionId))
            .ThrowsAsync(new KeyNotFoundException("Transaction not found"));

        // Act
        var actionResult = await _controller.GetTransaction(transactionId);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetTransaction_WhenUserNotOwner_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var transactionId = Guid.NewGuid();
        _transactionServiceMock.Setup(s => s.GetTransactionAsync(userId, transactionId))
            .ThrowsAsync(new UnauthorizedAccessException("Not the owner"));

        // Act
        var actionResult = await _controller.GetTransaction(transactionId);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    // ─── GetTransactions ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var filter = new TransactionFilterRequest();
        var pagedResult = new PagedResult<TransactionResponse>
        {
            Items = new List<TransactionResponse> { BuildResponse(userId) },
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
        _transactionServiceMock.Setup(s => s.GetUserTransactionsAsync(userId, filter))
            .ReturnsAsync(pagedResult);

        // Act
        var actionResult = await _controller.GetTransactions(filter);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(pagedResult);
    }

    // ─── DeleteTransaction ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTransaction_WhenValid_Returns204NoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var transactionId = Guid.NewGuid();
        _transactionServiceMock.Setup(s => s.DeleteTransactionAsync(userId, transactionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteTransaction(transactionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTransaction_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var transactionId = Guid.NewGuid();
        _transactionServiceMock.Setup(s => s.DeleteTransactionAsync(userId, transactionId))
            .ThrowsAsync(new KeyNotFoundException("Transaction not found"));

        // Act
        var result = await _controller.DeleteTransaction(transactionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteTransaction_WhenUserNotOwner_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var transactionId = Guid.NewGuid();
        _transactionServiceMock.Setup(s => s.DeleteTransactionAsync(userId, transactionId))
            .ThrowsAsync(new UnauthorizedAccessException("Not the owner"));

        // Act
        var result = await _controller.DeleteTransaction(transactionId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }
}
