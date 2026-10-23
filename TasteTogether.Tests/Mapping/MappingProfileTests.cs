using AutoMapper;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Service.Mapping;

namespace TasteTogether.Tests.Mapping;

public class MappingProfileTests
{
    private readonly MapperConfiguration _configuration;
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        // MapperConfiguration טוענת את ה-MappingProfile האמיתי של הפרויקט, בלי שום Mock
        _configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        // Assert: זורק חריגה אם יש שדה ב-DTO שאין לו מקור ולא סומן Ignore
        _configuration.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(UserRole.User, "User")]
    [InlineData(UserRole.Admin, "Admin")]
    public void Map_UserToUserResponse_RoleIsReturnedAsString(UserRole role, string expectedRole)
    {
        // Arrange
        var user = new User { Id = 1, Email = "dana@example.com", Role = role };

        // Act
        var response = _mapper.Map<UserResponse>(user);

        // Assert
        Assert.Equal(expectedRole, response.Role);
        Assert.Equal(1, response.Id);
        Assert.Equal("dana@example.com", response.Email);
    }

    [Theory]
    [InlineData(WorkshopStatus.Draft, "Draft")]
    [InlineData(WorkshopStatus.Published, "Published")]
    [InlineData(WorkshopStatus.Cancelled, "Cancelled")]
    public void Map_WorkshopToWorkshopResponse_StatusIsReturnedAsString(WorkshopStatus status, string expectedStatus)
    {
        // Arrange
        var workshop = new Workshop { Id = 1, Title = "Bread Workshop", Status = status };

        // Act
        var response = _mapper.Map<WorkshopResponse>(workshop);

        // Assert
        Assert.Equal(expectedStatus, response.Status);
        Assert.Equal("Bread Workshop", response.Title);
        Assert.Equal(workshop.Version, response.Version);
    }
}