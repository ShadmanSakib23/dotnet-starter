using Microsoft.AspNetCore.Identity;
using StarterApp.Interfaces;
using StarterApp.Models;
using StarterApp.Services;

namespace StarterApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
