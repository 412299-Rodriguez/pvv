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

// TODO Sprint 1: IngressRoutesSeeder (hosted service), LeadProjectionService,
//                SessionMiddleware, FingerprintMiddleware, TurnstileMiddleware,
//                RateLimiter, and the ingress/payments controllers.

// Health check endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pvv-bff" }));

app.Run();
