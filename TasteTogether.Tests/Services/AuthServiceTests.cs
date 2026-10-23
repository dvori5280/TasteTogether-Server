using AutoMapper;
using Moq;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;
using TasteTogether.Service;

namespace TasteTogether.Tests.Services;

public class AuthServiceTests
{
    private const string Password = "MySecret123!";

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _mapperMock.Object);
    }

    private static RegisterRequest ValidRegisterRequest() => new()
    {
        FullName = "Dana Cohen",
        Email = "dana@example.com",
        Password = Password
    };

    // מכינה את שלב יצירת הטוקן: המיפוי של המשתמשת ל-UserResponse ויצירת ה-LoginResponse.
    // מחזירה את ה-LoginResponse, כדי שהבדיקה תוכל לוודא שהוא מה שחזר
    private LoginResponse SetupTokenCreation()
    {
        var userResponse = new UserResponse { Id = 1, Email = "dana@example.com", Role = "User" };
        var loginResponse = new LoginResponse { Token = "test-token", User = userResponse };

        _mapperMock
            .Setup(m => m.Map<UserResponse>(It.IsAny<User>()))
            .Returns(userResponse);

        _tokenServiceMock
            .Setup(t => t.CreateToken(userResponse))
            .Returns(loginResponse);

        return loginResponse;
    }

    // מכינה הרשמה תקינה: האימייל פנוי, הסיסמה מגובבת, והמשתמשת נשמרת
    private LoginResponse SetupSuccessfulRegistration()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        return SetupTokenCreation();
    }

    // מכינה משתמשת קיימת עם גיבוב שמור, שנמצאת לפי האימייל התקין "dana@example.com"
    private void SetupExistingUser()
    {
        var user = new User
        {
            Id = 1,
            FullName = "Dana Cohen",
            Email = "dana@example.com",
            PasswordHash = "stored-hash"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("dana@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    // ---------- הרשמה ----------

    [Fact]
    public async Task RegisterAsync_NewEmail_StoresHashedPasswordAndNotThePlainOne()
    {
        // Arrange
        SetupSuccessfulRegistration();

        // Act
        await _service.RegisterAsync(ValidRegisterRequest(), CancellationToken.None);

        // Assert: נשמר הגיבוב, ולא הסיסמה עצמה
        _passwordHasherMock.Verify(h => h.Hash(Password), Times.Once);

        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.PasswordHash == "hashed-password" && u.PasswordHash != Password),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailWithSpacesAndCapitals_NormalizesEmailAndTrimsName()
    {
        // Arrange
        SetupSuccessfulRegistration();

        var request = ValidRegisterRequest();
        request.FullName = "  Dana Cohen  ";
        request.Email = "  Dana@Example.COM  ";

        // Act
        await _service.RegisterAsync(request, CancellationToken.None);

        // Assert: האימייל נשמר באותיות קטנות ובלי רווחים, והשם בלי רווחים בקצוות
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.Email == "dana@example.com" && u.FullName == "Dana Cohen"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_AlwaysAssignsUserRole()
    {
        // Arrange
        SetupSuccessfulRegistration();

        // Act
        await _service.RegisterAsync(ValidRegisterRequest(), CancellationToken.None);

        // Assert: מי שנרשמת בעצמה מקבלת תמיד תפקיד User, ולא Admin
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.Role == UserRole.User),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsTokenCreatedForTheNewUser()
    {
        // Arrange
        var loginResponse = SetupSuccessfulRegistration();

        // Act
        var result = await _service.RegisterAsync(ValidRegisterRequest(), CancellationToken.None);

        // Assert: מוחזר בדיוק ה-LoginResponse שיצר ה-TokenService
        Assert.Same(loginResponse, result);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegistered_ThrowsConflictException()
    {
        // Arrange: האימייל כבר קיים ב-DB
        SetupExistingUser();
        var request = ValidRegisterRequest();

        // Act + Assert: 409
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(request, CancellationToken.None));

        Assert.Equal("Email is already registered.", exception.Message);

        // לא נשמרה משתמשת ולא נוצר טוקן
        _userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _tokenServiceMock.Verify(t => t.CreateToken(It.IsAny<UserResponse>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_SameEmailWithDifferentCaseAndSpaces_ThrowsConflictException()
    {
        // Arrange: במסד קיים "dana@example.com", והבקשה מגיעה באותיות גדולות ועם רווחים
        SetupExistingUser();

        var request = ValidRegisterRequest();
        request.Email = "  DANA@Example.com ";

        // Act + Assert: גם כך מזהים שהאימייל כבר קיים
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.RegisterAsync(request, CancellationToken.None));
    }

    // ---------- התחברות ----------

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        SetupExistingUser();
        var loginResponse = SetupTokenCreation();

        _passwordHasherMock
            .Setup(h => h.Verify(Password, "stored-hash"))
            .Returns(true);

        var request = new LoginRequest { Email = "dana@example.com", Password = Password };

        // Act
        var result = await _service.LoginAsync(request, CancellationToken.None);

        // Assert
        Assert.Same(loginResponse, result);
    }

    [Fact]
    public async Task LoginAsync_EmailWithDifferentCaseAndSpaces_FindsTheUser()
    {
        // Arrange
        SetupExistingUser();
        SetupTokenCreation();

        _passwordHasherMock
            .Setup(h => h.Verify(Password, "stored-hash"))
            .Returns(true);

        var request = new LoginRequest { Email = "  DANA@Example.com ", Password = Password };

        // Act
        var result = await _service.LoginAsync(request, CancellationToken.None);

        // Assert: האימייל נורמל לפני החיפוש, ולכן המשתמשת נמצאה
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        // Arrange: אין משתמשת עם האימייל הזה
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest { Email = "nobody@example.com", Password = Password };

        // Act
        var result = await _service.LoginAsync(request, CancellationToken.None);

        // Assert: null (שבהמשך הופך ל-401), ולא נוצר טוקן
        Assert.Null(result);
        _tokenServiceMock.Verify(t => t.CreateToken(It.IsAny<UserResponse>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        // Arrange: המשתמשת קיימת, אבל הסיסמה לא מתאימה לגיבוב
        SetupExistingUser();

        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), "stored-hash"))
            .Returns(false);

        var request = new LoginRequest { Email = "dana@example.com", Password = "WrongPassword!" };

        // Act
        var result = await _service.LoginAsync(request, CancellationToken.None);

        // Assert: אותה תוצאה כמו משתמשת שלא קיימת, ולא נוצר טוקן
        Assert.Null(result);
        _tokenServiceMock.Verify(t => t.CreateToken(It.IsAny<UserResponse>()), Times.Never);
    }
}