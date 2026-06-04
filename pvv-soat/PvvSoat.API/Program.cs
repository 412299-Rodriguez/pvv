using Microsoft.EntityFrameworkCore;
using PvvSoat.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logging to console via Serilog.
builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Configuration is read from appsettings*.json and environment variables by the
// default host builder; no extra wiring needed here.

// EF Core — SQL Server (database pvv_soat_db).
builder.Services.AddDbContext<SoatDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Swagger / OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pvv-soat" }));

app.Run();
