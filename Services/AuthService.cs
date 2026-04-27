using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StarterApp.Data;
using StarterApp.DTOs;
using StarterApp.Exceptions;
using StarterApp.Models;

namespace StarterApp.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(
        ApplicationDbContext context, 
        IPasswordHasher<User> passwordHasher, 
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        try
        {
            var userExists = await UserExistsAsync(request.Email, request.Email);
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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} registered successfully with ID {UserId}",
                user.Email, user.Id);

            return new RegisterResponse
            {
                Id = user.Id,
                Username = user.Username ?? string.Empty,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string username, string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower() ||
                          (u.Username != null && u.Username.ToLower() == username.ToLower()));
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

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
                    LastName = user.LastName
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
            
            var user = await _context.Users.FindAsync(userId);
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
            new Claim("lastName", user.LastName ?? string.Empty)
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
        var refreshExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("type", "refresh")
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtIssuer,
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

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtIssuer,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out var validatedToken);
            
            var typeClaim = principal.Claims.FirstOrDefault(c => c.Type == "type");
            if (typeClaim?.Value != "refresh")
            {
                throw new SecurityTokenException("Not a refresh token");
            }

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new SecurityTokenException("Invalid user ID in token");
            }

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed");
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }
    }
}
