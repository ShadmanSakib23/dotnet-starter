using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Repositories;
using StarterApp.Services;

namespace StarterApp.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<ILogger<AnalyticsService>> _loggerMock;
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _loggerMock = new Mock<ILogger<AnalyticsService>>();
        _sut = new AnalyticsService(_transactionRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCategoryAnalyticsAsync_WhenNoDatesProvided_DefaultsToLast30Days()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;
        _transactionRepositoryMock
            .Setup(r => r.GetCategoryAnalyticsAsync(
                userId,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                null))
            .ReturnsAsync(new List<CategoryAnalyticsResponse>());

        // Act
        var result = await _sut.GetCategoryAnalyticsAsync(userId, null, null);
        var after = DateTime.UtcNow;

        // Assert
        result.DateRange.EndDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.DateRange.StartDate.Should().BeCloseTo(result.DateRange.EndDate.AddDays(-30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetCategoryAnalyticsAsync_SeparatesIncomeAndExpenseCategories()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categories = new List<CategoryAnalyticsResponse>
        {
            new() { Category = "Salary", Type = TransactionType.Income, TotalAmount = 3000m, TransactionCount = 1, AverageAmount = 3000m },
            new() { Category = "Food", Type = TransactionType.Expense, TotalAmount = 500m, TransactionCount = 2, AverageAmount = 250m },
            new() { Category = "Rent", Type = TransactionType.Expense, TotalAmount = 1000m, TransactionCount = 1, AverageAmount = 1000m }
        };
        _transactionRepositoryMock
            .Setup(r => r.GetCategoryAnalyticsAsync(userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
            .ReturnsAsync(categories);

        // Act
        var result = await _sut.GetCategoryAnalyticsAsync(userId, null, null);

        // Assert
        result.IncomeByCategory.Should().HaveCount(1);
        result.ExpenseByCategory.Should().HaveCount(2);
        result.IncomeByCategory.Should().AllSatisfy(c => c.Type.Should().Be(TransactionType.Income));
        result.ExpenseByCategory.Should().AllSatisfy(c => c.Type.Should().Be(TransactionType.Expense));
    }

    [Fact]
    public async Task GetCategoryAnalyticsAsync_CalculatesIncomePercentagesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categories = new List<CategoryAnalyticsResponse>
        {
            new() { Category = "Salary", Type = TransactionType.Income, TotalAmount = 3000m, TransactionCount = 1, AverageAmount = 3000m },
            new() { Category = "Freelance", Type = TransactionType.Income, TotalAmount = 1000m, TransactionCount = 1, AverageAmount = 1000m }
        };
        _transactionRepositoryMock
            .Setup(r => r.GetCategoryAnalyticsAsync(userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
            .ReturnsAsync(categories);

        // Act
        var result = await _sut.GetCategoryAnalyticsAsync(userId, null, null);

        // Assert
        // Total income = 4000; Salary = 75%, Freelance = 25%
        var salary = result.IncomeByCategory.Single(c => c.Category == "Salary");
        var freelance = result.IncomeByCategory.Single(c => c.Category == "Freelance");
        salary.Percentage.Should().Be(75.00m);
        freelance.Percentage.Should().Be(25.00m);
    }

    [Fact]
    public async Task GetCategoryAnalyticsAsync_CalculatesExpensePercentagesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categories = new List<CategoryAnalyticsResponse>
        {
            new() { Category = "Food", Type = TransactionType.Expense, TotalAmount = 400m, TransactionCount = 4, AverageAmount = 100m },
            new() { Category = "Rent", Type = TransactionType.Expense, TotalAmount = 600m, TransactionCount = 1, AverageAmount = 600m }
        };
        _transactionRepositoryMock
            .Setup(r => r.GetCategoryAnalyticsAsync(userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
            .ReturnsAsync(categories);

        // Act
        var result = await _sut.GetCategoryAnalyticsAsync(userId, null, null);

        // Assert
        // Total expenses = 1000; Food = 40%, Rent = 60%
        var food = result.ExpenseByCategory.Single(c => c.Category == "Food");
        var rent = result.ExpenseByCategory.Single(c => c.Category == "Rent");
        food.Percentage.Should().Be(40.00m);
        rent.Percentage.Should().Be(60.00m);
    }

    [Fact]
    public async Task GetMonthlyAnalyticsAsync_WithValueBelowMin_ClampsTo1()
    {
        // Arrange
        var userId = Guid.NewGuid();
        DateTime capturedStart = default;
        DateTime capturedEnd = default;
        _transactionRepositoryMock
            .Setup(r => r.GetMonthlyAnalyticsAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<Guid, DateTime, DateTime>((_, s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync(new List<MonthlyAnalyticsResponse>());

        // Act
        await _sut.GetMonthlyAnalyticsAsync(userId, 0);

        // Assert
        // months=0 clamped to 1: startDate = first day of current month (not in the future)
        capturedStart.Day.Should().Be(1); // normalized to first of month
        capturedStart.Should().BeBefore(capturedEnd.AddDays(1)); // not in the future
        capturedStart.Should().BeOnOrAfter(capturedEnd.AddMonths(-1)); // window is 1 month, not 0
    }

    [Fact]
    public async Task GetMonthlyAnalyticsAsync_WithValueAboveMax_ClampsTo24()
    {
        // Arrange
        var userId = Guid.NewGuid();
        DateTime capturedStart = default;
        DateTime capturedEnd = default;
        _transactionRepositoryMock
            .Setup(r => r.GetMonthlyAnalyticsAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<Guid, DateTime, DateTime>((_, s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync(new List<MonthlyAnalyticsResponse>());

        // Act
        await _sut.GetMonthlyAnalyticsAsync(userId, 100);

        // Assert
        // months=100 clamped to 24: startDate should be ~23 months ago, NOT 99 months ago
        capturedStart.Day.Should().Be(1); // normalized to first of month
        capturedStart.Should().BeOnOrAfter(capturedEnd.AddMonths(-25)); // within 25 months
        capturedStart.Should().BeBefore(capturedEnd.AddMonths(-20)); // at least 20 months ago
    }

    [Fact]
    public async Task GetMonthlyAnalyticsAsync_ReturnsMappedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var monthlyData = new List<MonthlyAnalyticsResponse>
        {
            new() { Year = 2026, Month = 4, MonthName = "April", TotalIncome = 3000m, TotalExpenses = 1000m, NetAmount = 2000m, TransactionCount = 5 },
            new() { Year = 2026, Month = 5, MonthName = "May", TotalIncome = 4000m, TotalExpenses = 1500m, NetAmount = 2500m, TransactionCount = 7 }
        };
        _transactionRepositoryMock
            .Setup(r => r.GetMonthlyAnalyticsAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(monthlyData);

        // Act
        var result = await _sut.GetMonthlyAnalyticsAsync(userId, 6);

        // Assert
        result.Should().HaveCount(2);
        result[0].MonthName.Should().Be("April");
        result[1].TotalIncome.Should().Be(4000m);
    }

    [Fact]
    public async Task GetSummaryAnalyticsAsync_WhenNoDatesProvided_DefaultsToLast30Days()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;
        var summary = new AnalyticsSummaryResponse
        {
            TotalIncome = 0m,
            TotalExpenses = 0m,
            NetAmount = 0m,
            TotalTransactions = 0,
            DateRange = new AnalyticsDateRange { StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow }
        };
        _transactionRepositoryMock
            .Setup(r => r.GetSummaryAnalyticsAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _sut.GetSummaryAnalyticsAsync(userId, null, null);
        var after = DateTime.UtcNow;

        // Assert
        result.DateRange.EndDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.DateRange.StartDate.Should().BeCloseTo(result.DateRange.EndDate.AddDays(-30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetSummaryAnalyticsAsync_SetsDateRangeOnResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var summary = new AnalyticsSummaryResponse
        {
            TotalIncome = 5000m,
            TotalExpenses = 2000m,
            NetAmount = 3000m,
            TotalTransactions = 10,
            DateRange = new AnalyticsDateRange { StartDate = DateTime.MinValue, EndDate = DateTime.MinValue }
        };
        _transactionRepositoryMock
            .Setup(r => r.GetSummaryAnalyticsAsync(userId, startDate, endDate))
            .ReturnsAsync(summary);

        // Act
        var result = await _sut.GetSummaryAnalyticsAsync(userId, startDate, endDate);

        // Assert
        result.DateRange.StartDate.Should().Be(startDate);
        result.DateRange.EndDate.Should().Be(endDate);
    }
}
