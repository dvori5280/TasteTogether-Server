using System.Diagnostics;

namespace TasteTogether.API.Middleware;

public class RequestLoggingMiddleware
{
    public const string CorrelationIdKey = "CorrelationId";
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();

        context.Items[CorrelationIdKey] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdKey] = correlationId
        });

        _logger.LogInformation("Incoming request {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation("{Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
}