using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TasteTogether.Data;

namespace TasteTogether.Tests.Concurrency;

// נוצרת פעם אחת לכל הבדיקות שמשתמשות במסד אמיתי: מוחקת את מסד הבדיקות ויוצרת אותו מחדש עם ה-Migrations
public class PostgresDatabaseFixture : IAsyncLifetime
{
    private DbContextOptions<TasteTogetherDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PostgresDatabaseFixture>()
            .Build();

        var connectionString = configuration.GetConnectionString("Tests")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Tests is missing in the User Secrets of TasteTogether.Tests.");

        // הגנה: לא ממשיכים אם זה לא מסד בדיקות, כדי שלא נמחק בטעות את מסד הפיתוח
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty;
        if (!databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Database name '{databaseName}' does not contain 'test'. Refusing to delete it.");

        _options = new DbContextOptionsBuilder<TasteTogetherDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // כל קריאה יוצרת DbContext חדש ונפרד, ובהמשך נשתמש בשניים כאלה בו-זמנית
    public TasteTogetherDbContext CreateContext() => new(_options);
}