using Microsoft.EntityFrameworkCore;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.Data;

public class DbSeeder
{
    private readonly TasteTogetherDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DbSeeder(TasteTogetherDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // יוצר נתוני דמו רק אם אין עדיין משתמשים, ולכן הפעלה חוזרת לא תיצור כפילויות
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
            return;

        var admin = new User
        {
            FullName = "Admin",
            Email = "admin@tt.com",
            PasswordHash = _passwordHasher.Hash("Admin123!"),
            Role = UserRole.Admin
        };

        var user1 = new User
        {
            FullName = "User 1",
            Email = "user1@tt.com",
            PasswordHash = _passwordHasher.Hash("User123!"),
            Role = UserRole.User
        };

        var user2 = new User
        {
            FullName = "User 2",
            Email = "user2@tt.com",
            PasswordHash = _passwordHasher.Hash("User123!"),
            Role = UserRole.User
        };

        var challah = new Recipe
        {
            Title = "Challah",
            Description = "Braided Shabbat bread",
            IngredientsText = new List<string> { "1 kg flour", "2 eggs", "1 tbsp dry yeast", "Sugar", "Salt" },
            InstructionsText = new List<string> { "Mix and knead the dough", "Let it rise", "Braid and bake" },
            User = admin
        };

        var pasta = new Recipe
        {
            Title = "Pasta",
            Description = "Fresh homemade pasta",
            IngredientsText = new List<string> { "400 g flour", "4 eggs", "Salt" },
            InstructionsText = new List<string> { "Make the dough", "Roll it thin", "Cut and boil" },
            User = admin
        };

        // סדנה מפורסמת עם שני מקומות בלבד, כדי שאפשר יהיה להדגים 409 של סדנה מלאה
        var challahClass = new Workshop
        {
            Title = "Challah Class",
            Description = "Learn to bake challah",
            Location = "Jerusalem",
            StartsAt = DateTime.UtcNow.AddDays(14),
            Capacity = 2,
            Status = WorkshopStatus.Published,
            Host = admin,
            Recipes = new List<Recipe> { challah }
        };

        // טיוטה, שגלויה רק לאדמינית
        var pastaClass = new Workshop
        {
            Title = "Pasta Class",
            Description = "Learn to make pasta",
            Location = "Tel Aviv",
            StartsAt = DateTime.UtcNow.AddDays(21),
            Capacity = 10,
            Status = WorkshopStatus.Draft,
            Host = admin,
            Recipes = new List<Recipe> { pasta }
        };

        _context.AddRange(admin, user1, user2, challah, pasta, challahClass, pastaClass);
        await _context.SaveChangesAsync(cancellationToken);
    }
}