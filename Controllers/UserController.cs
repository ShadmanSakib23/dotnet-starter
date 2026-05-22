

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using StarterApp.DTOs;
using StarterApp.Interfaces;
using StarterApp.Services;
using StarterApp.Enums;

namespace StarterApp.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    public UserController(
        IUserService userService
    )
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    [Authorize(Policy = "AnyRole")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> Retrieve()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("Invalid user token");
        }

        var result = await _userService.GetUserAsync(userId);
        return Ok(result);
    }

    [HttpPatch("profile")]
    [Authorize(Policy = "AnyRole")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> Update([FromBody] UpdateUserRequest request)
    {   
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("Invalid user token");
        }

        var result = await _userService.UpdateUserAsync(userId, request);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetAllUsersAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var result = await _userService.GetUserByIdAsync(userId, id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        await _userService.DeleteUserAsync(userId, id);
        return NoContent();
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var result = await _userService.AssignRoleAsync(userId, id, request.Role);
        return Ok(result);
    }
}