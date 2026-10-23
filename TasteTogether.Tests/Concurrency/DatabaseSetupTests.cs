using Microsoft.EntityFrameworkCore;

namespace TasteTogether.Tests.Concurrency;

[Collection("Postgres")]
public class DatabaseSetupTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public DatabaseSetupTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Database_AfterSetup_IsReachableAndFullyMigrated()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var canConnect = await context.Database.CanConnectAsync();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        // Assert: מחוברים למסד אמיתי, כל ה-Migrations הוחלו, ויש לפחות שתיים (כנדרש)
        Assert.True(canConnect);
        Assert.Empty(pendingMigrations);
        Assert.True(appliedMigrations.Count() >= 2);
    }
}