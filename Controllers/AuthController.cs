using Microsoft.AspNetCore.Mvc;
using StarterApp.DTOs;
using StarterApp.Exceptions;
using StarterApp.Services;

namespace StarterApp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterUserAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Registration conflict for email: {Email}", request.Email);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred during registration" });
        }
    }
}
