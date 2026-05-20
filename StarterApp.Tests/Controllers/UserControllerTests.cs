using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using StarterApp.Controllers;
using StarterApp.DTOs;
using StarterApp.Enums;
using StarterApp.Interfaces;

namespace StarterApp.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UserController(_userServiceMock.Object);
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

    private static UserResponse BuildUserResponse(Guid id)
    {
        return new UserResponse
        {
            Id = id,
            Email = "test@example.com",
            Username = "testuser",
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Role = "User"
        };
    }

    // ─── GetProfile (Retrieve) ────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WhenInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var actionResult = await _controller.Retrieve();
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetProfile_WhenFound_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var response = BuildUserResponse(userId);
        _userServiceMock.Setup(s => s.GetUserAsync(userId)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.Retrieve();
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetProfile_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        _userServiceMock.Setup(s => s.GetUserAsync(userId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var actionResult = await _controller.Retrieve();
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ─── UpdateProfile (Update) ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_WithValidData_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(userId);
        var request = new UpdateUserRequest { FirstName = "Jane", LastName = "Smith", Username = "janesmith" };
        var response = BuildUserResponse(userId);
        response.FirstName = "Jane";
        _userServiceMock.Setup(s => s.UpdateUserAsync(userId, request)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.Update(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task UpdateProfile_WhenInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new UpdateUserRequest { FirstName = "Jane" };

        // Act
        var actionResult = await _controller.Update(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>().Which.StatusCode.Should().Be(401);
    }

    // ─── GetAllUsers ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsers_Returns200Ok()
    {
        // Arrange
        var pagedResult = new PagedResult<UserResponse>
        {
            Items = new List<UserResponse> { BuildUserResponse(Guid.NewGuid()) },
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
        _userServiceMock.Setup(s => s.GetAllUsersAsync(1, 20)).ReturnsAsync(pagedResult);

        // Act
        var actionResult = await _controller.GetAllUsers();
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(pagedResult);
    }

    // ─── GetUserById ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_WhenInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var actionResult = await _controller.GetUserById(Guid.NewGuid());
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetUserById_WhenFound_Returns200Ok()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        var response = BuildUserResponse(targetId);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(adminId, targetId)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.GetUserById(targetId);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetUserById_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.GetUserByIdAsync(adminId, targetId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var actionResult = await _controller.GetUserById(targetId);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ─── DeleteUser ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_WhenInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _controller.DeleteUser(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteUser_WhenValid_Returns204NoContent()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(adminId, targetId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteUser(targetId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(adminId, targetId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var result = await _controller.DeleteUser(targetId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ─── AssignRole ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRole_WhenInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var request = new AssignRoleRequest { Role = UserRole.Admin };

        // Act
        var actionResult = await _controller.AssignRole(Guid.NewGuid(), request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task AssignRole_WithValidUpgrade_Returns200Ok()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        var request = new AssignRoleRequest { Role = UserRole.Admin };
        var response = BuildUserResponse(targetId);
        response.Role = "Admin";
        _userServiceMock.Setup(s => s.AssignRoleAsync(adminId, targetId, UserRole.Admin)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.AssignRole(targetId, request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task AssignRole_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        var request = new AssignRoleRequest { Role = UserRole.Admin };
        _userServiceMock.Setup(s => s.AssignRoleAsync(adminId, targetId, UserRole.Admin))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var actionResult = await _controller.AssignRole(targetId, request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AssignRole_WhenInvalidOperation_Returns400BadRequest()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _controller.ControllerContext = CreateControllerContext(adminId);
        var targetId = Guid.NewGuid();
        var request = new AssignRoleRequest { Role = UserRole.User };
        _userServiceMock.Setup(s => s.AssignRoleAsync(adminId, targetId, UserRole.User))
            .ThrowsAsync(new InvalidOperationException("Cannot downgrade role"));

        // Act
        var actionResult = await _controller.AssignRole(targetId, request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }
}
