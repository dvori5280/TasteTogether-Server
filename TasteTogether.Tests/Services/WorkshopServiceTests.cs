using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;
using TasteTogether.Service;

namespace TasteTogether.Tests.Services;

public partial class WorkshopServiceTests
{
    private const int WorkshopId = 1;

    private readonly Mock<IWorkshopRepository> _workshopRepositoryMock = new();
    private readonly Mock<IRegistrationRepository> _registrationRepositoryMock = new();
    private readonly Mock<IRecipeRepository> _recipeRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<WorkshopService>> _loggerMock = new();

    private readonly WorkshopService _service;

    public WorkshopServiceTests()
    {
        _service = new WorkshopService(
            _workshopRepositoryMock.Object,
            _registrationRepositoryMock.Object,
            _recipeRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    // מכינה סדנה אחת עם 3 נרשמים. started קובע אם מועדה כבר עבר (לפני שעה) או שהוא בעוד שבוע
    private Workshop SetupWorkshop(WorkshopStatus status, bool started = false)
    {
        var workshop = new Workshop
        {
            Id = WorkshopId,
            Title = "Bread Workshop",
            Status = status,
            StartsAt = started ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddDays(7)
        };

        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        _registrationRepositoryMock
            .Setup(r => r.CountByWorkshopAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _mapperMock
            .Setup(m => m.Map<WorkshopResponse>(It.IsAny<Workshop>()))
            .Returns(() => new WorkshopResponse());

        return workshop;
    }

    // מכינה את ה-Mocks של GetAllAsync: רשימת סדנאות, מספר כולל, וספירת נרשמים לכל סדנה
    private void SetupWorkshopList(List<Workshop> workshops, int totalCount, Dictionary<int, int> counts)
    {
        _workshopRepositoryMock
            .Setup(r => r.GetAllAsync(
                It.IsAny<WorkshopStatus?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(),
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshops);

        _workshopRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<WorkshopStatus?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);

        _registrationRepositoryMock
            .Setup(r => r.CountByWorkshopsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);

        _mapperMock
            .Setup(m => m.Map<WorkshopResponse>(It.IsAny<Workshop>()))
            .Returns(() => new WorkshopResponse());
    }

    // ---------- א. פרסום סדנה ----------

    [Fact]
    public async Task UpdateStatusAsync_PublishDraftWorkshop_PublishesAndSavesWorkshop()
    {
        // Arrange
        var workshop = SetupWorkshop(WorkshopStatus.Draft);
        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act
        var result = await _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None);

        // Assert
        Assert.Equal(WorkshopStatus.Published, workshop.Status);
        Assert.Equal(3, result.RegisteredCount);
        Assert.False(result.IsPast);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(workshop, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_PublishAlreadyPublishedWorkshop_ThrowsConflictException()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Published);
        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act + Assert: 409
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("הסדנה כבר פורסמה", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_PublishCancelledWorkshop_ThrowsConflictException()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Cancelled);
        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act + Assert: 409
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("לא ניתן לפרסם סדנה שבוטלה", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_PublishWorkshopThatAlreadyStarted_ThrowsBadRequestException()
    {
        // Arrange: טיוטה שמועדה כבר עבר
        SetupWorkshop(WorkshopStatus.Draft, started: true);
        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("לא ניתן לפרסם סדנה שמועדה כבר עבר", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- ב. ביטול סדנה ופסילות ----------

    [Fact]
    public async Task UpdateStatusAsync_CancelPublishedWorkshop_CancelsAndSavesWorkshop()
    {
        // Arrange
        var workshop = SetupWorkshop(WorkshopStatus.Published);
        var request = new UpdateWorkshopStatusRequest { Status = "Cancelled" };

        // Act
        var result = await _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None);

        // Assert
        Assert.Equal(WorkshopStatus.Cancelled, workshop.Status);
        Assert.Equal(3, result.RegisteredCount);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(workshop, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelAlreadyCancelledWorkshop_ThrowsConflictException()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Cancelled);
        var request = new UpdateWorkshopStatusRequest { Status = "Cancelled" };

        // Act + Assert: 409
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("הסדנה כבר בוטלה", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelWorkshopThatAlreadyStarted_ThrowsBadRequestException()
    {
        // Arrange: סדנה מפורסמת שכבר התחילה
        SetupWorkshop(WorkshopStatus.Published, started: true);
        var request = new UpdateWorkshopStatusRequest { Status = "Cancelled" };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("לא ניתן לבטל סדנה שכבר התחילה", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_StatusDraft_ThrowsBadRequestException()
    {
        // Arrange: אי אפשר להחזיר סדנה לטיוטה
        SetupWorkshop(WorkshopStatus.Published);
        var request = new UpdateWorkshopStatusRequest { Status = "Draft" };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("לא ניתן להחזיר סדנה לטיוטה", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Nonsense")]
    [InlineData("7")]
    public async Task UpdateStatusAsync_InvalidStatus_ThrowsBadRequestException(string invalidStatus)
    {
        // Arrange: טקסט שאינו סטטוס, או מספר שאינו ברשימת ה-Enum
        SetupWorkshop(WorkshopStatus.Published);
        var request = new UpdateWorkshopStatusRequest { Status = invalidStatus };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("הסטטוס אינו חוקי", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WorkshopNotFound_ThrowsNotFoundException()
    {
        // Arrange: הסדנה לא קיימת, ולכן ה-Repository מחזיר null
        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workshop?)null);

        var request = new UpdateWorkshopStatusRequest { Status = "Published" };

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateStatusAsync(WorkshopId, request, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);

        _workshopRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- ג. רשימת סדנאות ----------

    [Fact]
    public async Task GetAllAsync_AdminWithStatusFilter_UsesRequestedStatusAndNoDateLimit()
    {
        // Arrange
        SetupWorkshopList(new List<Workshop>(), totalCount: 0, counts: new Dictionary<int, int>());

        // Act: אדמינית מבקשת רק סדנאות שבוטלו
        await _service.GetAllAsync(WorkshopStatus.Cancelled, null, isAdmin: true, 1, 10, CancellationToken.None);

        // Assert: הסינון לפי הסטטוס שביקשה, ובלי הגבלת תאריך
        _workshopRepositoryMock.Verify(
            r => r.GetAllAsync(
                WorkshopStatus.Cancelled, null, null, It.IsAny<DateTime>(), 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_AdminWithoutStatus_SeesEverything()
    {
        // Arrange
        SetupWorkshopList(new List<Workshop>(), totalCount: 0, counts: new Dictionary<int, int>());

        // Act
        await _service.GetAllAsync(null, null, isAdmin: true, 1, 10, CancellationToken.None);

        // Assert: בלי סינון סטטוס ובלי הגבלת תאריך
        _workshopRepositoryMock.Verify(
            r => r.GetAllAsync(
                null, null, null, It.IsAny<DateTime>(), 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_RegularUser_SeesOnlyPublishedFromLast30Days()
    {
        // Arrange
        SetupWorkshopList(new List<Workshop>(), totalCount: 0, counts: new Dictionary<int, int>());

        // Act: משתמשת רגילה מנסה לבקש סדנאות שבוטלו
        await _service.GetAllAsync(WorkshopStatus.Cancelled, null, isAdmin: false, 1, 10, CancellationToken.None);

        // Assert: הסטטוס שביקשה נתעלם, ומתקבלות רק סדנאות Published מ-30 הימים האחרונים והלאה
        _workshopRepositoryMock.Verify(
            r => r.GetAllAsync(
                WorkshopStatus.Published,
                It.Is<DateTime?>(d => d.HasValue
                    && d.Value > DateTime.UtcNow.AddDays(-31)
                    && d.Value < DateTime.UtcNow.AddDays(-29)),
                null,
                It.IsAny<DateTime>(),
                1,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ValidRequest_ReturnsPagedResponseWithRegisteredCounts()
    {
        // Arrange: סדנה אחת בעמוד, ו-7 סדנאות בסך הכול. לסדנה 5 יש 3 נרשמים
        var workshop = new Workshop { Id = 5, Title = "Bread Workshop", Status = WorkshopStatus.Published };

        SetupWorkshopList(
            new List<Workshop> { workshop },
            totalCount: 7,
            counts: new Dictionary<int, int> { { 5, 3 } });

        // Act
        var result = await _service.GetAllAsync(null, null, isAdmin: false, 1, 10, CancellationToken.None);

        // Assert
        var item = Assert.Single(result.Items);
        Assert.Equal(3, item.RegisteredCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(7, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_PageIsZero_ThrowsBadRequestException()
    {
        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.GetAllAsync(null, null, isAdmin: false, 0, 10, CancellationToken.None));

        Assert.Equal("מספר העמוד חייב להיות 1 ומעלה", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task GetAllAsync_PageSizeOutOfRange_ThrowsBadRequestException(int pageSize)
    {
        // Act + Assert: 400 (המותר הוא בין 1 ל-20)
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.GetAllAsync(null, null, isAdmin: false, 1, pageSize, CancellationToken.None));

        Assert.Equal("גודל העמוד חייב להיות בין 1 ל-20", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_StatusNotInEnum_ThrowsBadRequestException()
    {
        // Arrange: ASP.NET מקבל גם מספר שאינו ברשימת ה-Enum, כמו 7
        var invalidStatus = (WorkshopStatus)7;

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.GetAllAsync(invalidStatus, null, isAdmin: true, 1, 10, CancellationToken.None));

        Assert.Equal("הסטטוס אינו חוקי", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_RecipeNotFound_ThrowsNotFoundException()
    {
        // Arrange: סינון לפי מתכון שלא קיים
        _recipeRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetAllAsync(null, 99, isAdmin: false, 1, 10, CancellationToken.None));

        Assert.Equal("המתכון לא נמצא", exception.Message);
    }

    // ---------- ד. נראות טיוטה ----------

    [Fact]
    public async Task GetByIdAsync_DraftWorkshopAndRegularUser_ReturnsNull()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Draft);

        // Act
        var result = await _service.GetByIdAsync(WorkshopId, isAdmin: false, CancellationToken.None);

        // Assert: טיוטה גלויה רק לאדמינית, ולכן עבור אחרות היא כאילו לא קיימת
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_DraftWorkshopAndAdmin_ReturnsWorkshopWithRegisteredCount()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Draft);

        // Act
        var result = await _service.GetByIdAsync(WorkshopId, isAdmin: true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.RegisteredCount);
    }

    [Fact]
    public async Task GetByIdAsync_PublishedWorkshopAndRegularUser_ReturnsWorkshopWithRegisteredCount()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Published);

        // Act
        var result = await _service.GetByIdAsync(WorkshopId, isAdmin: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.RegisteredCount);
        Assert.False(result.IsPast);
    }

    [Fact]
    public async Task GetByIdAsync_WorkshopNotFound_ReturnsNull()
    {
        // Arrange
        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workshop?)null);

        // Act
        var result = await _service.GetByIdAsync(WorkshopId, isAdmin: true, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecipesAsync_DraftWorkshopAndRegularUser_ThrowsNotFoundException()
    {
        // Arrange
        SetupWorkshop(WorkshopStatus.Draft);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetRecipesAsync(WorkshopId, isAdmin: false, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);
    }

    [Fact]
    public async Task GetRecipesAsync_WorkshopNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workshop?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetRecipesAsync(WorkshopId, isAdmin: true, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);
    }
}