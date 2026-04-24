using Microsoft.EntityFrameworkCore;
using StarterApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString
    ("DefaultConnection")));
var app = builder.Build();
 
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => 
{
    return Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow});
})
.WithName("HealthCheck");

app.MapGet("/db/test", async (ApplicationDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var dbName = db.Database.GetDbConnection().Database;
        
        return Results.Ok(new 
        { 
            connected = canConnect,
            database = dbName,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Database Connection Failed"
        );
    }
})
.WithName("DatabaseTest");

app.Run();
