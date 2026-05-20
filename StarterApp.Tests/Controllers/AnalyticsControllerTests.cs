using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using StarterApp.Controllers;
using StarterApp.DTOs;
using StarterApp.Interfaces;

namespace StarterApp.Tests.Controllers;

public class AnalyticsControllerTests
{
    private readonly Mock<IAnalyticsService> _analyticsServiceMock;
    private readonly Mock<ILogger<AnalyticsController>> _loggerMock;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _analyticsServiceMock = new Mock<IAnalyticsService>();
        _loggerMock = new Mock<ILogger<AnalyticsController>>();
        _controller = new AnalyticsController(_analyticsServiceMock.Object, _loggerMock.Object);
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

    // ─── GetCategoryAnalytics ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryAnalytics_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var response = new CategoryAnalyticsResult
        {
            IncomeByCategory = new List<CategoryAnalyticsResponse>(),
            ExpenseByCategory = new List<CategoryAnalyticsResponse>(),
            TotalIncome = 1000m,
            TotalExpenses = 500m,
            NetAmount = 500m,
            DateRange = new AnalyticsDateRange { StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow }
        };
        _analyticsServiceMock.Setup(s => s.GetCategoryAnalyticsAsync(userId, null, null))
            .ReturnsAsync(response);

        // Act
        var actionResult = await _controller.GetCategoryAnalytics(null, null);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetCategoryAnalytics_WhenUserIdMissing_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var actionResult = await _controller.GetCategoryAnalytics(null, null);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCategoryAnalytics_WhenException_Returns500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        _analyticsServiceMock.Setup(s => s.GetCategoryAnalyticsAsync(userId, null, null))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act
        var actionResult = await _controller.GetCategoryAnalytics(null, null);
        var result = actionResult.Result!;

        // Assert
        ((ObjectResult)result).StatusCode.Should().Be(500);
    }

    // ─── GetMonthlyAnalytics ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMonthlyAnalytics_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var response = new List<MonthlyAnalyticsResponse>
        {
            new MonthlyAnalyticsResponse
            {
                Year = 2026,
                Month = 5,
                MonthName = "May",
                TotalIncome = 2000m,
                TotalExpenses = 800m,
                NetAmount = 1200m,
                TransactionCount = 5
            }
        };
        _analyticsServiceMock.Setup(s => s.GetMonthlyAnalyticsAsync(userId, 6))
            .ReturnsAsync(response);

        // Act
        var actionResult = await _controller.GetMonthlyAnalytics(6);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetMonthlyAnalytics_WhenException_Returns500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        _analyticsServiceMock.Setup(s => s.GetMonthlyAnalyticsAsync(userId, 6))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act
        var actionResult = await _controller.GetMonthlyAnalytics(6);
        var result = actionResult.Result!;

        // Assert
        ((ObjectResult)result).StatusCode.Should().Be(500);
    }

    // ─── GetSummaryAnalytics ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAnalytics_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var response = new AnalyticsSummaryResponse
        {
            TotalIncome = 5000m,
            TotalExpenses = 2000m,
            NetAmount = 3000m,
            TotalTransactions = 10,
            IncomeTransactionCount = 6,
            ExpenseTransactionCount = 4,
            TopIncomeCategory = "Salary",
            TopExpenseCategory = "Food",
            DateRange = new AnalyticsDateRange { StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow }
        };
        _analyticsServiceMock.Setup(s => s.GetSummaryAnalyticsAsync(userId, null, null))
            .ReturnsAsync(response);

        // Act
        var actionResult = await _controller.GetSummaryAnalytics(null, null);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetSummaryAnalytics_WhenException_Returns500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        _analyticsServiceMock.Setup(s => s.GetSummaryAnalyticsAsync(userId, null, null))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act
        var actionResult = await _controller.GetSummaryAnalytics(null, null);
        var result = actionResult.Result!;

        // Assert
        ((ObjectResult)result).StatusCode.Should().Be(500);
    }
}
