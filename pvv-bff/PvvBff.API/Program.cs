using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PvvBff.API.Configuration;
using PvvBff.API.Middleware;
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

// RFC 7807 ProblemDetails for unhandled exceptions.
builder.Services.AddProblemDetails();

// CORS for the front (and admin) SPA origins.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("pvv-spa", policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders("X-Session-Id")));

// Security / session middleware options.
builder.Services.Configure<TurnstileOptions>(
    builder.Configuration.GetSection(TurnstileOptions.SectionName));
builder.Services.Configure<AnonymousSessionOptions>(
    builder.Configuration.GetSection(AnonymousSessionOptions.SectionName));

// Rate limiter — fixed window partitioned per session (falls back to IP).
var rateLimitOptions = builder.Configuration.GetSection(RateLimitOptions.SectionName)
    .Get<RateLimitOptions>() ?? new RateLimitOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ingress", httpContext =>
    {
        var partitionKey = httpContext.Items[SessionMiddleware.SessionItemKey] as string
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "global";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitOptions.PermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
            QueueLimit = 0,
        });
    });
});

// MVC controllers (ingress endpoint).
builder.Services.AddControllers();

// Swagger / OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Unhandled exceptions → RFC 7807 ProblemDetails (must wrap everything).
app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("pvv-spa");

app.UseAuthentication();
app.UseAuthorization();

// Ingress security/session pipeline (ingress paths only; see IngressPath):
//   Fingerprint → Turnstile → Session → RateLimiter
app.UseMiddleware<FingerprintMiddleware>();
app.UseMiddleware<TurnstileMiddleware>();
app.UseMiddleware<SessionMiddleware>();
app.UseRateLimiter();

// TODO HU-07/HU-08: lead-event and payments controllers.

// Ingress controller (POST /api/ingress).
app.MapControllers();

// Health check endpoint.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pvv-bff" }));

app.Run();
