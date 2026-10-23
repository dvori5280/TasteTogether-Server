using Microsoft.Extensions.Logging;
using Moq;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Exceptions;
using TasteTogether.Tests.Helpers;

namespace TasteTogether.Tests.Services;

// אותה מחלקה כמו RegistrationServiceTests (partial)
public partial class RegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_ValidRequest_LogsRegistrationWithIdsOnly()
    {
        // Arrange
        var workshop = SetupValidRegistrationScenario(registeredCount: 4);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act
        await _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None);

        // Assert: N4, שורת Information עם מזהים בלבד (בלי אימייל, שם או טוקן)
        _loggerMock.VerifyLog(
            LogLevel.Information,
            $"User {UserId} registered to workshop {WorkshopId}",
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WorkshopIsFull_DoesNotLogRegistration()
    {
        // Arrange: הסדנה מלאה, והרשמה נדחית
        var workshop = SetupValidRegistrationScenario(registeredCount: 5);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        // Assert: לא נכתבה שורה שאומרת שההרשמה הצליחה
        _loggerMock.VerifyLog(
            LogLevel.Information,
            $"User {UserId} registered to workshop {WorkshopId}",
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_StaleWorkshopVersion_LogsWarningWithIdsOnly()
    {
        // Arrange: יש מקום פנוי, אבל ה-Version שנשלח אינו של הסדנה
        SetupValidRegistrationScenario(registeredCount: 2);
        var request = new PlaceRegistrationRequest { Version = Guid.NewGuid() };

        // Act
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        // Assert: N5, שורת Warning עם מזהים בלבד
        _loggerMock.VerifyLog(
            LogLevel.Warning,
            $"Stale workshop version: user {UserId} tried to register to workshop {WorkshopId} with an outdated version",
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ValidRegistration_LogsCancellationWithIdsOnly()
    {
        // Arrange
        SetupValidCancelScenario();

        // Act
        await _service.CancelAsync(WorkshopId, UserId, CancellationToken.None);

        // Assert: N4, שורת Information עם מזהים בלבד
        _loggerMock.VerifyLog(
            LogLevel.Information,
            $"User {UserId} cancelled registration to workshop {WorkshopId}",
            Times.Once);
    }
}