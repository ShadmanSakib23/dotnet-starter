using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApp.DTOs;
using StarterApp.Interfaces;

namespace StarterApp.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "AnyRole")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get analytics grouped by category with income/expense percentages
    /// </summary>
    /// <param name="startDate">Optional start date (defaults to 30 days ago)</param>
    /// <param name="endDate">Optional end date (defaults to now)</param>
    [HttpGet("by-category")]
    [ProducesResponseType(typeof(CategoryAnalyticsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryAnalyticsResult>> GetCategoryAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }

        try
        {
            var result = await _analyticsService.GetCategoryAnalyticsAsync(userId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category analytics for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving category analytics" });
        }
    }

    /// <summary>
    /// Get analytics grouped by month showing income/expense trends
    /// </summary>
    /// <param name="months">Number of months to retrieve (default: 6, max: 24)</param>
    [HttpGet("by-month")]
    [ProducesResponseType(typeof(List<MonthlyAnalyticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MonthlyAnalyticsResponse>>> GetMonthlyAnalytics(
        [FromQuery] int months = 6)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }

        try
        {
            var result = await _analyticsService.GetMonthlyAnalyticsAsync(userId, months);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly analytics for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving monthly analytics" });
        }
    }

    /// <summary>
    /// Get summary analytics with totals and top categories
    /// </summary>
    /// <param name="startDate">Optional start date (defaults to 30 days ago)</param>
    /// <param name="endDate">Optional end date (defaults to now)</param>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AnalyticsSummaryResponse>> GetSummaryAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }
        
        try
        {
            var result = await _analyticsService.GetSummaryAnalyticsAsync(userId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving summary analytics for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving summary analytics" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}
