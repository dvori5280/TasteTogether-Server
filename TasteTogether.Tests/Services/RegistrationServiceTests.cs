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

public partial class RegistrationServiceTests
{
    private const int WorkshopId = 1;
    private const int UserId = 10;
    private const string UserEmail = "dana@example.com";
    private const string UserFullName = "Dana Cohen";

    private readonly Mock<IRegistrationRepository> _registrationRepositoryMock = new();
    private readonly Mock<IWorkshopRepository> _workshopRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<RegistrationService>> _loggerMock = new();

    private readonly RegistrationService _service;

    public RegistrationServiceTests()
    {
        _service = new RegistrationService(
            _registrationRepositoryMock.Object,
            _workshopRepositoryMock.Object,
            _userRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    // מכינה תרחיש תקין של הרשמה לסדנה עם 5 מקומות.
    // registeredCount הוא כמה מקומות כבר תפוסים. כל בדיקה משנה רק את מה שמבדיל אותה
    private Workshop SetupValidRegistrationScenario(int registeredCount)
    {
        var workshop = new Workshop
        {
            Id = WorkshopId,
            Title = "Bread Workshop",
            Capacity = 5,
            Status = WorkshopStatus.Published,
            StartsAt = DateTime.UtcNow.AddDays(7),
            Version = Guid.NewGuid()
        };

        var user = new User
        {
            Id = UserId,
            FullName = UserFullName,
            Email = UserEmail
        };

        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _registrationRepositoryMock
            .Setup(r => r.GetByUserAndWorkshopAsync(UserId, WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkshopRegistration?)null);

        _registrationRepositoryMock
            .Setup(r => r.CountByWorkshopAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registeredCount);

        _registrationRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WorkshopRegistration>(), workshop, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkshopRegistration registration, Workshop w, CancellationToken ct) => registration);

        _mapperMock
            .Setup(m => m.Map<RegistrationResponse>(It.IsAny<WorkshopRegistration>()))
            .Returns(new RegistrationResponse());

        return workshop;
    }

    // מכינה תרחיש תקין של ביטול הרשמה: המשתמשת רשומה לסדנה שעדיין לא התחילה
    private Workshop SetupValidCancelScenario()
    {
        var workshop = new Workshop
        {
            Id = WorkshopId,
            Title = "Bread Workshop",
            Status = WorkshopStatus.Published,
            StartsAt = DateTime.UtcNow.AddDays(7)
        };

        var registration = new WorkshopRegistration
        {
            UserId = UserId,
            WorkshopId = WorkshopId
        };

        _registrationRepositoryMock
            .Setup(r => r.GetByUserAndWorkshopAsync(UserId, WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registration);

        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        return workshop;
    }

    [Fact]
    public async Task RegisterAsync_LastSeatAvailable_RegistersUserSuccessfully()
    {
        // Arrange: 4 מתוך 5 מקומות תפוסים, כלומר נשאר מקום אחרון
        var workshop = SetupValidRegistrationScenario(registeredCount: 4);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act
        var result = await _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None);

        // Assert
        Assert.Equal(workshop.Title, result.WorkshopTitle);
        Assert.Equal("Published", result.WorkshopStatus);
        Assert.Equal(UserEmail, result.UserEmail);
        Assert.Equal(UserFullName, result.UserFullName);

        _registrationRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<WorkshopRegistration>(x => x.UserId == UserId && x.WorkshopId == WorkshopId),
                workshop,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WorkshopIsFull_ThrowsConflictException()
    {
        // Arrange: 5 מתוך 5 מקומות תפוסים, כלומר הסדנה מלאה
        var workshop = SetupValidRegistrationScenario(registeredCount: 5);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act + Assert: ההרשמה חייבת להיזרק כ-ConflictException (409)
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("הסדנה מלאה", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_UserAlreadyRegistered_ThrowsConflictException()
    {
        // Arrange: יש מקום פנוי (2 מתוך 5), אבל המשתמשת כבר רשומה לסדנה
        var workshop = SetupValidRegistrationScenario(registeredCount: 2);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        _registrationRepositoryMock
            .Setup(r => r.GetByUserAndWorkshopAsync(UserId, WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkshopRegistration { UserId = UserId, WorkshopId = WorkshopId });

        // Act + Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("כבר נרשמת לסדנה זו", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_StaleWorkshopVersion_ThrowsConflictException()
    {
        // Arrange: יש מקום פנוי (2 מתוך 5), אבל הלקוחה שולחת Version שאינו של הסדנה כיום
        SetupValidRegistrationScenario(registeredCount: 2);
        var request = new PlaceRegistrationRequest { Version = Guid.NewGuid() };

        // Act + Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("הסדנה השתנתה מאז שהצגת אותה. רענני ונסי שוב", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_MissingVersion_ThrowsBadRequestException()
    {
        // Arrange: הבקשה בלי Version (null), למשל כשה-Service נקרא ישירות בלי ולידציה של ה-DTO
        SetupValidRegistrationScenario(registeredCount: 2);
        var request = new PlaceRegistrationRequest { Version = null };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("יש לשלוח את ה-Version של הסדנה", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WorkshopNotFound_ThrowsNotFoundException()
    {
        // Arrange: הסדנה לא קיימת, ולכן ה-Repository מחזיר null
        SetupValidRegistrationScenario(registeredCount: 2);
        var request = new PlaceRegistrationRequest { Version = Guid.NewGuid() };

        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workshop?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange: המשתמשת לא קיימת, ולכן ה-Repository מחזיר null
        var workshop = SetupValidRegistrationScenario(registeredCount: 2);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("המשתמש לא נמצא", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WorkshopIsDraft_ThrowsNotFoundException()
    {
        // Arrange: טיוטה גלויה רק לאדמינית, ולכן עבור משתמשת רגילה היא כאילו לא קיימת
        var workshop = SetupValidRegistrationScenario(registeredCount: 2);
        workshop.Status = WorkshopStatus.Draft;
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WorkshopIsCancelled_ThrowsBadRequestException()
    {
        // Arrange: הסדנה בוטלה
        var workshop = SetupValidRegistrationScenario(registeredCount: 2);
        workshop.Status = WorkshopStatus.Cancelled;
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("הסדנה בוטלה", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WorkshopAlreadyStarted_ThrowsBadRequestException()
    {
        // Arrange: הסדנה מפורסמת, אבל זמן ההתחלה שלה היה לפני שעה
        var workshop = SetupValidRegistrationScenario(registeredCount: 2);
        workshop.StartsAt = DateTime.UtcNow.AddHours(-1);
        var request = new PlaceRegistrationRequest { Version = workshop.Version };

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.RegisterAsync(WorkshopId, UserId, request, CancellationToken.None));

        Assert.Equal("הסדנה כבר התחילה", exception.Message);

        // לא נשמרה שום הרשמה
        _registrationRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<Workshop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ValidRegistration_DeletesRegistration()
    {
        // Arrange
        SetupValidCancelScenario();

        // Act
        await _service.CancelAsync(WorkshopId, UserId, CancellationToken.None);

        // Assert: ההרשמה של המשתמשת לסדנה נמחקה פעם אחת
        _registrationRepositoryMock.Verify(
            r => r.DeleteAsync(
                It.Is<WorkshopRegistration>(x => x.UserId == UserId && x.WorkshopId == WorkshopId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_RegistrationNotFound_ThrowsNotFoundException()
    {
        // Arrange: המשתמשת לא רשומה לסדנה, ולכן ה-Repository מחזיר null
        SetupValidCancelScenario();

        _registrationRepositoryMock
            .Setup(r => r.GetByUserAndWorkshopAsync(UserId, WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkshopRegistration?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CancelAsync(WorkshopId, UserId, CancellationToken.None));

        Assert.Equal("ההרשמה לא נמצאה", exception.Message);

        // שום הרשמה לא נמחקה
        _registrationRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WorkshopNotFound_ThrowsNotFoundException()
    {
        // Arrange: ההרשמה קיימת אבל הסדנה לא נמצאת
        SetupValidCancelScenario();

        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workshop?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CancelAsync(WorkshopId, UserId, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);

        // שום הרשמה לא נמחקה
        _registrationRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WorkshopAlreadyStarted_ThrowsBadRequestException()
    {
        // Arrange: הסדנה התחילה לפני שעה, ולכן אי אפשר לבטל הרשמה
        var workshop = SetupValidCancelScenario();
        workshop.StartsAt = DateTime.UtcNow.AddHours(-1);

        // Act + Assert: 400
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CancelAsync(WorkshopId, UserId, CancellationToken.None));

        Assert.Equal("לא ניתן לבטל הרשמה לאחר תחילת הסדנה", exception.Message);

        // שום הרשמה לא נמחקה
        _registrationRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<WorkshopRegistration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}