using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PvvConfig.Application.Exceptions;

namespace PvvConfig.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and maps domain exceptions to ProblemDetails
/// responses with the appropriate HTTP status code.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            UnauthorizedException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            logger.LogError(ex, "Unhandled exception");
        }
        else
        {
            logger.LogWarning("{Title}: {Message}", title, ex.Message);
        }

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = status == HttpStatusCode.InternalServerError
                ? "An unexpected error occurred."
                : ex.Message
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
