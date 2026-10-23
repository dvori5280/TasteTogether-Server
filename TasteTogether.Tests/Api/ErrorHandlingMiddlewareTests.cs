using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TasteTogether.API.Middleware;
using TasteTogether.Core.Exceptions;
using TasteTogether.Tests.Helpers;

namespace TasteTogether.Tests.Api;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock = new();

    // בקשה מדומה שהתשובה שלה נכתבת לזיכרון, כך שאפשר לקרוא אותה בבדיקה
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    // מריצה את ה-Middleware כשהשלב הבא ב-Pipeline זורק את החריגה שקיבלנו
    private async Task<DefaultHttpContext> InvokeWithExceptionAsync(Exception exception)
    {
        var context = CreateContext();
        var middleware = new ErrorHandlingMiddleware(_ => throw exception, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        return context;
    }

    private static string ReadBodyText(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404WithProblemDetails()
    {
        // Act
        var context = await InvokeWithExceptionAsync(new NotFoundException("The workshop was not found"));

        // Assert
        var body = JsonDocument.Parse(ReadBodyText(context)).RootElement;

        Assert.Equal(404, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.GetProperty("title").GetString());
        Assert.Equal("The workshop was not found", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ConflictException_Returns409WithProblemDetails()
    {
        // Act
        var context = await InvokeWithExceptionAsync(new ConflictException("The workshop is full"));

        // Assert
        var body = JsonDocument.Parse(ReadBodyText(context)).RootElement;

        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("Conflict", body.GetProperty("title").GetString());
        Assert.Equal("The workshop is full", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_BadRequestException_Returns400WithProblemDetails()
    {
        // Act
        var context = await InvokeWithExceptionAsync(new BadRequestException("The status is invalid"));

        // Assert
        var body = JsonDocument.Parse(ReadBodyText(context)).RootElement;

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("Bad Request", body.GetProperty("title").GetString());
        Assert.Equal("The status is invalid", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_Returns500WithoutExposingTheRealMessage()
    {
        // Arrange: חריגה לא צפויה שההודעה שלה מכילה מידע רגיש
        var exception = new InvalidOperationException("Connection failed: Password=secret123");

        // Act
        var context = await InvokeWithExceptionAsync(exception);

        // Assert: הלקוחה מקבלת הודעה כללית בלבד
        var bodyText = ReadBodyText(context);
        var body = JsonDocument.Parse(bodyText).RootElement;

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred.", body.GetProperty("detail").GetString());
        Assert.DoesNotContain("secret123", bodyText);
    }

    [Fact]
    public async Task InvokeAsync_NoException_LeavesTheResponseUntouched()
    {
        // Arrange
        var context = CreateContext();
        var middleware = new ErrorHandlingMiddleware(_ => Task.CompletedTask, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_RequestCancelledByClient_Returns499()
    {
        // Arrange: הלקוחה ביטלה את הבקשה באמצע
        using var cancellationTokenSource = new CancellationTokenSource();
        var context = CreateContext();
        context.RequestAborted = cancellationTokenSource.Token;
        cancellationTokenSource.Cancel();

        var middleware = new ErrorHandlingMiddleware(
            _ => throw new OperationCanceledException(), _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_LogsError()
    {
        // Act
        await InvokeWithExceptionAsync(new InvalidOperationException("Something broke"));

        // Assert: חריגה לא צפויה נרשמת ברמת Error
        _loggerMock.VerifyLog(LogLevel.Error, "Unhandled exception", Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_LogsWarningAndNotError()
    {
        // Act
        await InvokeWithExceptionAsync(new NotFoundException("The workshop was not found"));

        // Assert: חריגה עסקית נרשמת ברמת Warning, ולא כ-Error
        _loggerMock.VerifyLog(
            LogLevel.Warning,
            "Handled exception NotFoundException: The workshop was not found",
            Times.Once);

        _loggerMock.VerifyLog(LogLevel.Error, "Unhandled exception", Times.Never);
    }
}