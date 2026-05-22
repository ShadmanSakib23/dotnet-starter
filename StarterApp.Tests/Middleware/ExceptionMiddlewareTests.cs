using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using StarterApp.Exceptions;
using StarterApp.Middleware;
using System.Text.Json;

namespace StarterApp.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock;

    public ExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
    }

    private ExceptionMiddleware CreateMiddleware(Exception exceptionToThrow)
    {
        RequestDelegate next = _ => throw exceptionToThrow;
        return new ExceptionMiddleware(next, _loggerMock.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    [Fact]
    public async Task ExceptionMiddleware_KeyNotFoundException_Returns404WithProblemDetails()
    {
        var exception = new KeyNotFoundException("Resource not found");
        var middleware = CreateMiddleware(exception);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().Be("Resource not found");
    }

    [Fact]
    public async Task ExceptionMiddleware_ConflictException_Returns409WithProblemDetails()
    {
        var exception = new ConflictException("Conflict occurred");
        var middleware = CreateMiddleware(exception);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(409);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.GetProperty("status").GetInt32().Should().Be(409);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().Be("Conflict occurred");
    }

    [Fact]
    public async Task ExceptionMiddleware_AuthenticationException_Returns401WithProblemDetails()
    {
        var exception = new StarterApp.Exceptions.AuthenticationException("Invalid credentials");
        var middleware = CreateMiddleware(exception);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.GetProperty("status").GetInt32().Should().Be(401);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task ExceptionMiddleware_UnauthorizedAccessException_Returns403WithProblemDetails()
    {
        var exception = new UnauthorizedAccessException("Access denied");
        var middleware = CreateMiddleware(exception);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.GetProperty("status").GetInt32().Should().Be(403);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().Be("Access denied");
    }

    [Fact]
    public async Task ExceptionMiddleware_InvalidOperationException_Returns400WithProblemDetails()
    {
        var exception = new InvalidOperationException("Bad request");
        var middleware = CreateMiddleware(exception);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().Be("Bad request");
    }

    [Fact]
    public async Task ExceptionMiddleware_UnhandledException_Returns500WithProblemDetails()
    {
        var exception = new Exception("Something went wrong");
        var middleware = CreateMiddleware(exception);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Contain("application/problem+json");

        var body = await ReadResponseBody(context);
        body.GetProperty("status").GetInt32().Should().Be(500);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().Be("Something went wrong");
    }
}
