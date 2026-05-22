using Microsoft.AspNetCore.Mvc;
using StarterApp.Exceptions;
using System.Text.Json;

namespace StarterApp.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            StarterApp.Exceptions.AuthenticationException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Handled exception ({StatusCode}): {Message}", statusCode, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
