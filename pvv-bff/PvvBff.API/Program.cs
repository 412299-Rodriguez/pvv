using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PvvBff.Application;
using PvvBff.Infrastructure;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logging to console via Serilog.
builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Application + Infrastructure layers (MediatR, Redis, MongoDB).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT authentication skeleton (the BFF validates tokens issued by pvv-config).
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = jwtSection["Secret"];
var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwtSecret) ? new string('0', 32) : jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// MVC controllers (ingress endpoint).
builder.Services.AddControllers();

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

app.UseAuthentication();
app.UseAuthorization();

// TODO HU-06/6B: Fingerprint, Turnstile, Session middlewares + RateLimiter.
// TODO HU-07/HU-08: lead-event and payments controllers.

// Ingress controller (POST /api/ingress).
app.MapControllers();

// Health check endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pvv-bff" }));

app.Run();
