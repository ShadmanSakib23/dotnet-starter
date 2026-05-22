using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using StarterApp.Controllers;
using StarterApp.DTOs;
using StarterApp.Exceptions;
using StarterApp.Interfaces;

namespace StarterApp.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    // ─── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        // Arrange
        var request = new RegisterRequest { Email = "test@example.com", Password = "Password1!" };
        var response = new RegisterResponse
        {
            Id = Guid.NewGuid(),
            Username = "test",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };
        _authServiceMock.Setup(s => s.RegisterUserAsync(request)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.Register(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        ((ObjectResult)result).Value.Should().Be(response);
    }

    [Fact]
    public async Task Register_WhenEmailConflict_Returns409Conflict()
    {
        // Arrange
        var request = new RegisterRequest { Email = "dupe@example.com", Password = "Password1!" };
        _authServiceMock.Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new ConflictException("Email already in use"));

        // Act & Assert — middleware handles ConflictException → 409
        var act = async () => await _controller.Register(request);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Register_WhenUnexpectedException_Returns500()
    {
        // Arrange
        var request = new RegisterRequest { Email = "test@example.com", Password = "Password1!" };
        _authServiceMock.Setup(s => s.RegisterUserAsync(request))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act & Assert — middleware handles unhandled exceptions → 500
        var act = async () => await _controller.Register(request);
        await act.Should().ThrowAsync<Exception>();
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password1!" };
        var response = new LoginResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _authServiceMock.Setup(s => s.LoginAsync(request)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.Login(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_PropagatesAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest { Email = "bad@example.com", Password = "wrong" };
        _authServiceMock.Setup(s => s.LoginAsync(request))
            .ThrowsAsync(new AuthenticationException("Invalid credentials"));

        // Act & Assert — middleware handles AuthenticationException → 401
        var act = async () => await _controller.Login(request);
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Login_WhenUnexpectedException_Returns500()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password1!" };
        _authServiceMock.Setup(s => s.LoginAsync(request))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act & Assert — middleware handles unhandled exceptions → 500
        var act = async () => await _controller.Login(request);
        await act.Should().ThrowAsync<Exception>();
    }

    // ─── Refresh ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidToken_Returns200Ok()
    {
        // Arrange
        var request = new RefreshRequest { RefreshToken = "valid-refresh-token" };
        var response = new RefreshResponse
        {
            AccessToken = "new-access",
            RefreshToken = "new-refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _authServiceMock.Setup(s => s.RefreshTokenAsync(request)).ReturnsAsync(response);

        // Act
        var actionResult = await _controller.Refresh(request);
        var result = actionResult.Result!;

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_PropagatesAuthenticationException()
    {
        // Arrange
        var request = new RefreshRequest { RefreshToken = "bad-token" };
        _authServiceMock.Setup(s => s.RefreshTokenAsync(request))
            .ThrowsAsync(new AuthenticationException("Invalid refresh token"));

        // Act & Assert — middleware handles AuthenticationException → 401
        var act = async () => await _controller.Refresh(request);
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Refresh_WhenUnexpectedException_Returns500()
    {
        // Arrange
        var request = new RefreshRequest { RefreshToken = "bad-token" };
        _authServiceMock.Setup(s => s.RefreshTokenAsync(request))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act & Assert — middleware handles unhandled exceptions → 500
        var act = async () => await _controller.Refresh(request);
        await act.Should().ThrowAsync<Exception>();
    }
}
