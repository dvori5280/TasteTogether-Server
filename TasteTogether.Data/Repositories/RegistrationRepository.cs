using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.Data;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly TasteTogetherDbContext _context;
    private readonly ILogger<RegistrationRepository> _logger;

    public RegistrationRepository(
        TasteTogetherDbContext context,
        ILogger<RegistrationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkshopRegistration?> GetByUserAndWorkshopAsync(
        int userId, int workshopId, CancellationToken cancellationToken)
    {
        return await _context.WorkshopRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WorkshopId == workshopId, cancellationToken);
    }

    public async Task<List<WorkshopRegistration>> GetByUserAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.WorkshopRegistrations
            .AsNoTracking()
            .Include(r => r.Workshop)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkshopRegistration>> GetByWorkshopAsync(int workshopId, CancellationToken cancellationToken)
    {
        return await _context.WorkshopRegistrations
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.WorkshopId == workshopId)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByWorkshopAsync(int workshopId, CancellationToken cancellationToken)
    {
        return await _context.WorkshopRegistrations
            .AsNoTracking()
            .CountAsync(r => r.WorkshopId == workshopId, cancellationToken);
    }

    public async Task<Dictionary<int, int>> CountByWorkshopsAsync(
        List<int> workshopIds, CancellationToken cancellationToken)
    {
        return await _context.WorkshopRegistrations
            .AsNoTracking()
            .Where(r => workshopIds.Contains(r.WorkshopId))
            .GroupBy(r => r.WorkshopId)
            .Select(g => new { WorkshopId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.WorkshopId, x => x.Count, cancellationToken);
    }

    // שמירה אחת שכוללת את ההרשמה ואת החלפת ה-Version של הסדנה.
    // SaveChangesAsync אחד הוא טרנזקציה: או ששניהם נשמרים, או שאף אחד
    public async Task<WorkshopRegistration> AddAsync(
        WorkshopRegistration registration, Workshop workshop, CancellationToken cancellationToken)
    {
        _context.WorkshopRegistrations.Add(registration);

        // מצרפים את הסדנה שנקראה (עם ה-Version שלה) ומסמנים שרק ה-Version שונה.
        // ב-DbContext הערך מוחלף לחדש, וב-UPDATE נכנס WHERE Version = הערך שנקרא
        _context.Workshops.Attach(workshop);
        _context.Entry(workshop).Property(w => w.Version).IsModified = true;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        // חייב לבוא לפני ה-catch של DbUpdateException, כי היא יורשת ממנה
        catch (DbUpdateConcurrencyException)
        {
            // מישהי אחרת שינתה את הסדנה בין הקריאה לשמירה. ההרשמה לא נשמרה.
            // רושמים רק מזהים, בלי מידע רגיש
            _logger.LogWarning(
                "Concurrency conflict on workshop {WorkshopId} while user {UserId} tried to register",
                registration.WorkshopId, registration.UserId);

            throw new ConflictException("הסדנה נתפסה על ידי משתמשת אחרת. נסי שוב");
        }
        // חדש: הרשמה כפולה בו-זמנית. האינדקס הייחודי ב-PostgreSQL דוחה אותה בקוד 23505
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _logger.LogWarning(
                "Duplicate registration blocked by unique index for user {UserId} on workshop {WorkshopId}",
                registration.UserId, registration.WorkshopId);

            throw new ConflictException("כבר נרשמת לסדנה זו");
        }

        return registration;
    }

    public async Task DeleteAsync(WorkshopRegistration registration, CancellationToken cancellationToken)
    {
        _context.WorkshopRegistrations.Remove(registration);
        await _context.SaveChangesAsync(cancellationToken);
    }
}