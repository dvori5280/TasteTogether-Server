using Microsoft.AspNetCore.Mvc;
using TasteTogether.Core.Exceptions;

namespace TasteTogether.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            using var scope = BeginCorrelationScope(context);
            _logger.LogInformation("Request was cancelled by the client");
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        using var scope = BeginCorrelationScope(context);

        var (status, title, detail) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(ex, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning("Handled exception {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
        problem.Extensions["correlationId"] = GetCorrelationId(context);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }

    private static string? GetCorrelationId(HttpContext context)
    {
        return context.Items[RequestLoggingMiddleware.CorrelationIdKey]?.ToString();
    }

    private IDisposable? BeginCorrelationScope(HttpContext context)
    {
        return _logger.BeginScope(new Dictionary<string, object>
        {
            [RequestLoggingMiddleware.CorrelationIdKey] = GetCorrelationId(context) ?? string.Empty
        });
    }
}