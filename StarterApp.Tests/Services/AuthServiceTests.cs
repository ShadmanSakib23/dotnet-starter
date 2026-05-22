using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StarterApp.DTOs;
using StarterApp.Exceptions;
using StarterApp.Models;
using StarterApp.Repositories;
using StarterApp.Services;
using StarterApp.Tests.Helpers;

namespace StarterApp.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _configuration = CreateJwtConfig();
        _sut = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object,
            _configuration);
    }

    private static IConfiguration CreateJwtConfig()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "super_secret_key_for_testing_1234567890!!" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" },
            { "Jwt:AccessTokenExpirationMinutes", "15" },
            { "Jwt:RefreshTokenExpirationDays", "7" }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
    }

    [Fact]
    public async Task RegisterUserAsync_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!"
        };
        _userRepositoryMock
            .Setup(r => r.UserExistsAsync(request.Email, request.Email))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.RegisterUserAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidData_ReturnsRegisterResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "Password123!"
        };
        _userRepositoryMock
            .Setup(r => r.UserExistsAsync(request.Email, request.Email))
            .ReturnsAsync(false);
        _passwordHasherMock
            .Setup(h => h.HashPassword(null!, request.Password))
            .Returns("hashed_password");
        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _sut.RegisterUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidData_HashesPassword()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "jane@example.com",
            Password = "Password123!"
        };
        _userRepositoryMock
            .Setup(r => r.UserExistsAsync(request.Email, request.Email))
            .ReturnsAsync(false);
        _passwordHasherMock
            .Setup(h => h.HashPassword(null!, request.Password))
            .Returns("hashed_password");
        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        await _sut.RegisterUserAsync(request);

        // Assert
        _passwordHasherMock.Verify(h => h.HashPassword(null!, request.Password), Times.Once());
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest { Email = "notfound@example.com", Password = "any" };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsAuthenticationException()
    {
        // Arrange
        var user = UserFactory.Create(email: "user@example.com");
        var request = new LoginRequest { Email = user.Email, Password = "wrong_password" };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Failed);

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponseWithTokens()
    {
        // Arrange
        var user = UserFactory.Create(email: "user@example.com");
        var request = new LoginRequest { Email = user.Email, Password = "correct_password" };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new RefreshRequest { RefreshToken = "this.is.not.a.valid.token" };

        // Act
        var act = async () => await _sut.RefreshTokenAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest { Email = "notfound@example.com", Password = "any" };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new RefreshRequest { RefreshToken = "this.is.not.a.valid.token" };

        // Act
        var act = async () => await _sut.RefreshTokenAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange — login first to obtain a valid refresh token
        var user = UserFactory.Create();
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "password"))
            .Returns(PasswordVerificationResult.Success);

        var loginResponse = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "password" });
        var validRefreshToken = loginResponse.RefreshToken;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        var refreshRequest = new RefreshRequest { RefreshToken = validRefreshToken };

        // Act
        var result = await _sut.RefreshTokenAsync(refreshRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }
}
