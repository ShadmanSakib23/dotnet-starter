using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApp.DTOs;
using StarterApp.Interfaces;

namespace StarterApp.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize(Policy = "AnyRole")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        ITransactionService transactionService,
        ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }

        var result = await _transactionService.CreateTransactionAsync(userId, request);
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }

        var result = await _transactionService.GetTransactionAsync(userId, id);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> GetTransactions([FromQuery] TransactionFilterRequest filter)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }

        var result = await _transactionService.GetUserTransactionsAsync(userId, filter);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized("Invalid user token");
        }

        await _transactionService.DeleteTransactionAsync(userId, id);
        return NoContent();
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
