using Microsoft.Extensions.Logging;
using Moq;

namespace TasteTogether.Tests.Helpers;

public static class LoggerMockExtensions
{
    // בודקת שנכתבה שורת לוג אחת בדיוק עם הרמה והטקסט שביקשנו
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> loggerMock, LogLevel level, string expectedMessage, Func<Times> times)
    {
        loggerMock.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == expectedMessage),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
    }
}