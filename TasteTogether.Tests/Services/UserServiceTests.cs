using AutoMapper;
using Moq;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Interfaces;
using TasteTogether.Service;

namespace TasteTogether.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_userRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_UsersExist_ReturnsMappedResponses()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Email = "dana@example.com" },
            new() { Id = 2, Email = "noa@example.com" }
        };

        var responses = new List<UserResponse>
        {
            new() { Id = 1, Email = "dana@example.com", Role = "User" },
            new() { Id = 2, Email = "noa@example.com", Role = "Admin" }
        };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        _mapperMock
            .Setup(m => m.Map<List<UserResponse>>(users))
            .Returns(responses);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert: מוחזרת בדיוק הרשימה שה-Mapper יצר מהמשתמשות של ה-Repository
        Assert.Same(responses, result);
    }

    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsMappedResponse()
    {
        // Arrange
        var user = new User { Id = 1, Email = "dana@example.com" };
        var response = new UserResponse { Id = 1, Email = "dana@example.com", Role = "User" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mapperMock
            .Setup(m => m.Map<UserResponse>(user))
            .Returns(response);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.Same(response, result);
    }

    [Fact]
    public async Task GetByIdAsync_UserNotFound_ReturnsNullWithoutMapping()
    {
        // Arrange: המשתמשת לא קיימת, ולכן ה-Repository מחזיר null
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert: מוחזר null (שבהמשך הופך ל-404), ולא נעשה מיפוי
        Assert.Null(result);
        _mapperMock.Verify(m => m.Map<UserResponse>(It.IsAny<object>()), Times.Never);
    }
}