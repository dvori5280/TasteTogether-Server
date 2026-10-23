using Microsoft.EntityFrameworkCore;
using TasteTogether.Core.Entities;

namespace TasteTogether.Data
{
    public class TasteTogetherDbContext : DbContext
    {
        public TasteTogetherDbContext(DbContextOptions<TasteTogetherDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Workshop> Workshops { get; set; }
        public DbSet<WorkshopRegistration> WorkshopRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Recipe: משתמש אחד, הרבה מתכונים
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Workshop - Host (User): מארח אחד, הרבה סדנאות
            modelBuilder.Entity<Workshop>()
                .HasOne(w => w.Host)
                .WithMany()
                .HasForeignKey(w => w.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkshopRegistration - User: משתמש אחד, הרבה הרשמות
            modelBuilder.Entity<WorkshopRegistration>()
                .HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkshopRegistration - Workshop: סדנה אחת, הרבה הרשמות
            modelBuilder.Entity<WorkshopRegistration>()
                .HasOne(r => r.Workshop)
                .WithMany(w => w.Registrations)
                .HasForeignKey(r => r.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);

            // Recipe - Workshop: Many-to-Many אוטומטי (EF Core יוצר את טבלת הקשר)
            modelBuilder.Entity<Recipe>()
                .HasMany(r => r.Workshops)
                .WithMany(w => w.Recipes);

            // אינדקס ייחודי: אימייל אחד לכל משתמש
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // אינדקס ייחודי: משתמש לא יכול להירשם פעמיים לאותה סדנה
            modelBuilder.Entity<WorkshopRegistration>()
                .HasIndex(r => new { r.UserId, r.WorkshopId })
                .IsUnique();

            // Enums נשמרים כטקסט
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Workshop>()
                .Property(w => w.Status)
                .HasConversion<string>();

            // חדש: Version הוא concurrency token. EF מוסיף את הערך הישן שלו ל-WHERE בכל עדכון של סדנה
            modelBuilder.Entity<Workshop>()
                .Property(w => w.Version)
                .IsConcurrencyToken();
        }

        // חדש: כל סדנה שמתעדכנת מקבלת Version חדש.
        // הערך הישן (Original) נשאר כפי שנקרא, ו-EF משתמש בו בתנאי ה-WHERE
        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<Workshop>()
                         .Where(e => e.State == EntityState.Modified))
            {
                entry.Property(w => w.Version).CurrentValue = Guid.NewGuid();
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}