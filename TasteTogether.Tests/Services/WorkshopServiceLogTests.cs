using Microsoft.Extensions.Logging;
using Moq;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using TasteTogether.Tests.Helpers;

namespace TasteTogether.Tests.Services;

// אותה מחלקה כמו WorkshopServiceTests (partial)
public partial class WorkshopServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_PublishDraftWorkshop_LogsStatusChangeWithIdsOnly()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Draft);
        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act
        await _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None);

        // Assert: N4, שורת Information עם מזהה הסדנה והסטטוס החדש בלבד
        _loggerMock.VerifyLog(
            LogLevel.Information,
            $"Workshop {WorkshopId} status changed to Published",
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelPublishedWorkshop_LogsStatusChangeWithIdsOnly()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Published);
        var request = new UpdateWorkshopStatusRequest { Status = "Cancelled" };

        // Act
        await _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None);

        // Assert
        _loggerMock.VerifyLog(
            LogLevel.Information,
            $"Workshop {WorkshopId} status changed to Cancelled",
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_PublishAlreadyPublishedWorkshop_DoesNotLogStatusChange()
    {
        // Arrange: הסדנה כבר מפורסמת, והבקשה נדחית
        SetupWorkshop(WorkshopStatus.Published);
        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        // Assert: לא נכתבה שורה שאומרת שהסטטוס השתנה
        _loggerMock.VerifyLog(
            LogLevel.Information,
            $"Workshop {WorkshopId} status changed to Published",
            Times.Never);
    }
}