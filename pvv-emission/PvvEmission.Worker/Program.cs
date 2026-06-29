using PvvEmission.Infrastructure;
using PvvEmission.Worker;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logging to console via Serilog.
builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Infrastructure layer (MongoDB + RabbitMQ connection factory).
builder.Services.AddInfrastructure(builder.Configuration);

// Emission retry policy + the background service.
builder.Services.Configure<EmissionOptions>(
    builder.Configuration.GetSection(EmissionOptions.SectionName));
builder.Services.AddHostedService<EmissionWorker>();

var app = builder.Build();

// Health check endpoint (no Swagger for this worker service).
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pvv-emission" }));

app.Run();
