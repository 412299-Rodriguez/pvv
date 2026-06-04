using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PvvSoat.API.Middleware;
using PvvSoat.Application;
using PvvSoat.Infrastructure;
using PvvSoat.Infrastructure.BackgroundJobs;
using PvvSoat.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logging to console via Serilog.
builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Application + Infrastructure layers (MediatR, EF Core, repositories, unit of work).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Background job: expires budgets whose validity window has passed.
builder.Services.AddHostedService<BudgetExpirationJob>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger / OpenAPI (with XML comments).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Apply pending migrations and seed development data on startup.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SoatDbContext>();
    await context.Database.MigrateAsync();
    await SoatDbSeeder.SeedAsync(context, app.Environment);
}

app.UseSerilogRequestLogging();

// Map domain exceptions to ProblemDetails responses.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health check endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pvv-soat" }));

app.Run();
