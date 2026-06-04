using PvvConfig.Application;
using PvvConfig.Infrastructure;
using PvvConfig.Worker;
using Serilog;
using Serilog.Formatting.Json;

var builder = Host.CreateApplicationBuilder(args);

// Structured JSON logging to console via Serilog.
builder.Services.AddSerilog((_, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Application + Infrastructure layers (EF Core, Redis).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The cache synchronization background service.
builder.Services.AddHostedService<CacheSyncWorker>();

var host = builder.Build();
host.Run();
