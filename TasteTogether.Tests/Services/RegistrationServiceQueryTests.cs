using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using Moq;

namespace TasteTogether.Tests.Services;

// זו אותה מחלקה כמו RegistrationServiceTests (partial), ולכן יש גישה לשדות, ל-constructor ולקבועים שלה
public partial class RegistrationServiceTests
{
    [Fact]
    public async Task GetByUserAsync_UserHasRegistrations_ReturnsResponsesWithWorkshopDetails()
    {
        // Arrange: למשתמשת שתי הרשמות, לשתי סדנאות שונות
        var user = new User { Id = UserId, FullName = UserFullName, Email = UserEmail };

        var registrations = new List<WorkshopRegistration>
        {
            new()
            {
                Id = 1, UserId = UserId, WorkshopId = 1,
                Workshop = new Workshop { Title = "Bread Workshop", Status = WorkshopStatus.Published }
            },
            new()
            {
                Id = 2, UserId = UserId, WorkshopId = 2,
                Workshop = new Workshop { Title = "Pasta Workshop", Status = WorkshopStatus.Cancelled }
            }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _registrationRepositoryMock
            .Setup(r => r.GetByUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrations);

        _mapperMock
            .Setup(m => m.Map<RegistrationResponse>(It.IsAny<WorkshopRegistration>()))
            .Returns(() => new RegistrationResponse());

        // Act
        var result = await _service.GetByUserAsync(UserId, CancellationToken.None);

        // Assert: שתי תשובות, וה-Service מילא בכל אחת את פרטי הסדנה ואת פרטי המשתמשת
        Assert.Equal(2, result.Count);

        Assert.Equal("Bread Workshop", result[0].WorkshopTitle);
        Assert.Equal("Published", result[0].WorkshopStatus);
        Assert.Equal(UserEmail, result[0].UserEmail);
        Assert.Equal(UserFullName, result[0].UserFullName);

        Assert.Equal("Pasta Workshop", result[1].WorkshopTitle);
        Assert.Equal("Cancelled", result[1].WorkshopStatus);
    }

    [Fact]
    public async Task GetByUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange: המשתמשת לא קיימת, ולכן ה-Repository מחזיר null
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByUserAsync(UserId, CancellationToken.None));

        Assert.Equal("המשתמש לא נמצא", exception.Message);

        // לא נשלפו הרשמות
        _registrationRepositoryMock.Verify(
            r => r.GetByUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByWorkshopAsync_WorkshopHasRegistrations_ReturnsResponsesWithUserDetails()
    {
        // Arrange: בסדנה שתי נרשמות, כל אחת עם פרטים משלה
        var workshop = new Workshop { Id = WorkshopId, Title = "Bread Workshop", Status = WorkshopStatus.Published };

        var registrations = new List<WorkshopRegistration>
        {
            new()
            {
                Id = 1, UserId = 10, WorkshopId = WorkshopId,
                User = new User { Id = 10, Email = "dana@example.com", FullName = "Dana Cohen" }
            },
            new()
            {
                Id = 2, UserId = 11, WorkshopId = WorkshopId,
                User = new User { Id = 11, Email = "noa@example.com", FullName = "Noa Levi" }
            }
        };

        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        _registrationRepositoryMock
            .Setup(r => r.GetByWorkshopAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrations);

        _mapperMock
            .Setup(m => m.Map<RegistrationResponse>(It.IsAny<WorkshopRegistration>()))
            .Returns(() => new RegistrationResponse());

        // Act
        var result = await _service.GetByWorkshopAsync(WorkshopId, CancellationToken.None);

        // Assert: שתי תשובות, עם כותרת הסדנה ופרטי כל נרשמת
        Assert.Equal(2, result.Count);

        Assert.Equal("Bread Workshop", result[0].WorkshopTitle);
        Assert.Equal("dana@example.com", result[0].UserEmail);
        Assert.Equal("Dana Cohen", result[0].UserFullName);

        Assert.Equal("Bread Workshop", result[1].WorkshopTitle);
        Assert.Equal("noa@example.com", result[1].UserEmail);
        Assert.Equal("Noa Levi", result[1].UserFullName);
    }

    [Fact]
    public async Task GetByWorkshopAsync_WorkshopNotFound_ThrowsNotFoundException()
    {
        // Arrange: הסדנה לא קיימת, ולכן ה-Repository מחזיר null
        _workshopRepositoryMock
            .Setup(r => r.GetByIdAsync(WorkshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workshop?)null);

        // Act + Assert: 404
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByWorkshopAsync(WorkshopId, CancellationToken.None));

        Assert.Equal("הסדנה לא נמצאה", exception.Message);

        // לא נשלפו הרשמות
        _registrationRepositoryMock.Verify(
            r => r.GetByWorkshopAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}