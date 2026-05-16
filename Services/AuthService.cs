using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StarterApp.DTOs;
using StarterApp.Exceptions;
using StarterApp.Interfaces;
using StarterApp.Models;
using StarterApp.Repositories;

namespace StarterApp.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher, 
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        try
        {
            var userExists = await _userRepository.UserExistsAsync(request.Email, request.Email);
            if (userExists)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                throw new ConflictException("Email already exists");
            }

            var passwordHash = _passwordHasher.HashPassword(null!, request.Password);
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = passwordHash
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("User {Email} registered successfully with ID {UserId}",
                user.Email, user.Id);

            return new RegisterResponse
            {
                Id = user.Id,
                Username = user.Username ?? string.Empty,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                Role = user.Role.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"));

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            throw;
        }
    }

    public async Task<RefreshResponse> RefreshTokenAsync(RefreshRequest request)
    {
        try
        {
            var userId = ValidateRefreshToken(request.RefreshToken);
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token");
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes"));

            _logger.LogInformation("Refresh token renewed for user {UserId}", user.Id);

            return new RefreshResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }
    }

    private string GenerateAccessToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        var expirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName ?? string.Empty),
            new Claim("lastName", user.LastName ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        var refreshExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");

        _logger.LogInformation("Generating refresh token. Issuer: {Issuer}, Audience: {Audience}, ExpiryDays: {Days}", 
            jwtIssuer, jwtAudience, refreshExpirationDays);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("type", "refresh")
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(refreshExpirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private Guid ValidateRefreshToken(string refreshToken)
    {
        var jwtSecret = _configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        _logger.LogInformation("Validating refresh token. Expected Issuer: {Issuer}, Expected Audience: {Audience}", 
            jwtIssuer, jwtAudience);

        var tokenHandler = new JwtSecurityTokenHandler();
        // Disable claim type mapping to preserve original claim names
        tokenHandler.InboundClaimTypeMap.Clear();
        
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        // First, decode without validation to see what's in the token
        var jwtToken = tokenHandler.ReadJwtToken(refreshToken);
        _logger.LogInformation("Token contents - Issuer: {Issuer}, Audience: {Audience}, Expiry: {Expiry}", 
            jwtToken.Issuer, 
            string.Join(", ", jwtToken.Audiences), 
            jwtToken.ValidTo);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out var validatedToken);
            
            var typeClaim = principal.Claims.FirstOrDefault(c => c.Type == "type");
            if (typeClaim?.Value != "refresh")
            {
                _logger.LogWarning("Token type claim mismatch. Expected 'refresh', got '{Type}'", typeClaim?.Value);
                throw new SecurityTokenException("Not a refresh token");
            }

            // After clearing InboundClaimTypeMap, "sub" stays as "sub"
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid user ID in token. Available claims: {Claims}", 
                    string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}")));
                throw new SecurityTokenException("Invalid user ID in token");
            }

            _logger.LogInformation("Refresh token validated successfully for user {UserId}", userId);
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed: {Message}", ex.Message);
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }
    }
}
