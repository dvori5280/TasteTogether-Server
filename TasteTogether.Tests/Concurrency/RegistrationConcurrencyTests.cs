using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;
using TasteTogether.Data;
using TasteTogether.Service;
using TasteTogether.Service.Mapping;

namespace TasteTogether.Tests.Concurrency;

[Collection("Postgres")]
public class RegistrationConcurrencyTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private readonly PostgresDatabaseFixture _fixture;

    public RegistrationConcurrencyTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // הנתונים שנוצרו במסד לבדיקה: סדנה ושתי משתמשות
    private sealed record SeedData(int WorkshopId, Guid WorkshopVersion, int UserAId, int UserBId);

    // יוצרת במסד האמיתי מארחת, שתי משתמשות וסדנה מפורסמת. אימיילים ייחודיים, כדי שהבדיקות לא יתנגשו
    private async Task<SeedData> SeedWorkshopWithTwoUsersAsync(int capacity)
    {
        await using var context = _fixture.CreateContext();

        var host = new User
        {
            FullName = "Host",
            Email = $"host-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };

        var userA = new User
        {
            FullName = "User A",
            Email = $"a-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash"
        };

        var userB = new User
        {
            FullName = "User B",
            Email = $"b-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash"
        };

        var workshop = new Workshop
        {
            Title = "Concurrency Workshop",
            Location = "Jerusalem",
            StartsAt = DateTime.UtcNow.AddDays(7),
            Capacity = capacity,
            Status = WorkshopStatus.Published,
            Host = host
        };

        context.AddRange(host, userA, userB, workshop);
        await context.SaveChangesAsync();

        return new SeedData(workshop.Id, workshop.Version, userA.Id, userB.Id);
    }

    // בונה RegistrationService אמיתי על DbContext נתון: Repositories אמיתיים, Mapper אמיתי.
    // רק חיפוש המשתמשת הוא Mock, כי המשתמשות כבר קיימות במסד ואין צורך לקרוא להן שוב
    private static RegistrationService CreateRegistrationService(TasteTogetherDbContext context)
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => new User
            {
                Id = id,
                FullName = $"User {id}",
                Email = $"user{id}@example.com"
            });

        return new RegistrationService(
            new RegistrationRepository(context, NullLogger<RegistrationRepository>.Instance),
            new WorkshopRepository(context, NullLogger<WorkshopRepository>.Instance),
            userRepositoryMock.Object,
            Mapper,
            NullLogger<RegistrationService>.Instance);
    }

    // מריצה הרשמה ומחזירה את החריגה אם נזרקה, או null אם ההרשמה הצליחה
    private static async Task<Exception?> TryRegisterAsync(
        RegistrationService service, int workshopId, int userId, PlaceRegistrationRequest request)
    {
        try
        {
            await service.RegisterAsync(workshopId, userId, request, CancellationToken.None);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact]
    public async Task RegisterAsync_TwoUsersCompeteForLastSeat_ExactlyOneSucceedsAndOtherGetsConflict()
    {
        // Arrange: סדנה עם מקום אחד בלבד, ושתי משתמשות שראו אותה עם אותו Version
        var seed = await SeedWorkshopWithTwoUsersAsync(capacity: 1);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var serviceA = CreateRegistrationService(contextA);
        var serviceB = CreateRegistrationService(contextB);

        var request = new PlaceRegistrationRequest { Version = seed.WorkshopVersion };

        // Act: שתי ההרשמות יוצאות בו-זמנית, כל אחת עם DbContext משלה
        var results = await Task.WhenAll(
            TryRegisterAsync(serviceA, seed.WorkshopId, seed.UserAId, request),
            TryRegisterAsync(serviceB, seed.WorkshopId, seed.UserBId, request));

        // Assert: הצלחה אחת (בלי חריגה), וכישלון אחד עם ConflictException (409)
        Assert.Single(results, r => r == null);
        Assert.Single(results, r => r is ConflictException);

        // במסד נשמרה הרשמה אחת בלבד
        await using var verifyContext = _fixture.CreateContext();
        var registrationCount = await verifyContext.WorkshopRegistrations
            .CountAsync(r => r.WorkshopId == seed.WorkshopId);

        Assert.Equal(1, registrationCount);
    }

    [Fact]
    public async Task AddAsync_TwoDbContextsReadSameWorkshopVersion_SecondSaveThrowsConflictAndSavesNothing()
    {
        // Arrange
        var seed = await SeedWorkshopWithTwoUsersAsync(capacity: 1);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var workshopRepositoryA = new WorkshopRepository(contextA, NullLogger<WorkshopRepository>.Instance);
        var workshopRepositoryB = new WorkshopRepository(contextB, NullLogger<WorkshopRepository>.Instance);
        var registrationRepositoryA = new RegistrationRepository(contextA, NullLogger<RegistrationRepository>.Instance);
        var registrationRepositoryB = new RegistrationRepository(contextB, NullLogger<RegistrationRepository>.Instance);

        // שני ה-DbContexts קוראים את אותה סדנה, ומקבלים את אותו Version
        var workshopA = await workshopRepositoryA.GetByIdAsync(seed.WorkshopId, CancellationToken.None);
        var workshopB = await workshopRepositoryB.GetByIdAsync(seed.WorkshopId, CancellationToken.None);

        Assert.NotNull(workshopA);
        Assert.NotNull(workshopB);
        Assert.Equal(workshopA.Version, workshopB.Version);

        // Act 1: המשתמשת הראשונה שומרת בהצלחה, וה-Version של הסדנה במסד מוחלף
        await registrationRepositoryA.AddAsync(
            new WorkshopRegistration { UserId = seed.UserAId, WorkshopId = seed.WorkshopId },
            workshopA,
            CancellationToken.None);

        // Act 2 + Assert: השנייה שומרת עם Version שכבר לא עדכני, ולכן נדחית
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => registrationRepositoryB.AddAsync(
                new WorkshopRegistration { UserId = seed.UserBId, WorkshopId = seed.WorkshopId },
                workshopB,
                CancellationToken.None));

        Assert.Equal("הסדנה נתפסה על ידי משתמשת אחרת. נסי שוב", exception.Message);

        // במסד: רק ההרשמה של הראשונה, וה-Version של הסדנה השתנה
        await using var verifyContext = _fixture.CreateContext();

        var registrations = await verifyContext.WorkshopRegistrations
            .Where(r => r.WorkshopId == seed.WorkshopId)
            .ToListAsync();

        var saved = Assert.Single(registrations);
        Assert.Equal(seed.UserAId, saved.UserId);

        var workshopAfter = await verifyContext.Workshops
            .AsNoTracking()
            .SingleAsync(w => w.Id == seed.WorkshopId);

        Assert.NotEqual(seed.WorkshopVersion, workshopAfter.Version);
    }
}