using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.Data;

public class WorkshopRepository : IWorkshopRepository
{
    private readonly TasteTogetherDbContext _context;
    private readonly ILogger<WorkshopRepository> _logger;

    public WorkshopRepository(
        TasteTogetherDbContext context,
        ILogger<WorkshopRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Workshop?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Workshops
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<List<Workshop>> GetAllAsync(
    WorkshopStatus? status, DateTime? startsFrom, int? recipeId, DateTime now, int page, int pageSize,
    CancellationToken cancellationToken)
    {
        // קודם סדנאות שעוד לא התחילו (false), ואחריהן שעברו (true). בכל קבוצה לפי מועד ואז Id
        return await ApplyFilters(status, startsFrom, recipeId)
            .OrderBy(w => w.StartsAt < now)
            .ThenBy(w => w.StartsAt)
            .ThenBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        WorkshopStatus? status, DateTime? startsFrom, int? recipeId, CancellationToken cancellationToken)
    {
        return await ApplyFilters(status, startsFrom, recipeId).CountAsync(cancellationToken);
    }

    // אותו סינון גם לרשימה וגם לספירה, כדי שהסך הכול יתאים תמיד לתוצאות
    private IQueryable<Workshop> ApplyFilters(WorkshopStatus? status, DateTime? startsFrom, int? recipeId)
    {
        var query = _context.Workshops.AsNoTracking();

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        if (startsFrom.HasValue)
            query = query.Where(w => w.StartsAt >= startsFrom.Value);

        if (recipeId.HasValue)
            query = query.Where(w => w.Recipes.Any(r => r.Id == recipeId.Value));

        return query;
    }

    public async Task<Workshop> AddAsync(Workshop workshop, CancellationToken cancellationToken)
    {
        _context.Workshops.Add(workshop);
        await _context.SaveChangesAsync(cancellationToken);
        return workshop;
    }

    public async Task UpdateAsync(Workshop workshop, CancellationToken cancellationToken)
    {
        // הישות נטענה בלי מעקב, ולכן Update אומר ל-EF שהיא קיימת ושונתה
        _context.Workshops.Update(workshop);

        // חדש: תופסים רק את חריגת ה-Concurrency. ה-UPDATE כולל WHERE Version = הערך שנקרא
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // מישהי אחרת עדכנה את הסדנה בין הקריאה לשמירה. רושמים רק מזהה, בלי מידע רגיש
            _logger.LogWarning("Concurrency conflict while updating workshop {WorkshopId}", workshop.Id);

            throw new ConflictException("הסדנה עודכנה על ידי משתמשת אחרת. רענני ונסי שוב");
        }
    }

    public async Task<List<Workshop>> GetByRecipeAsync(int recipeId, CancellationToken cancellationToken)
    {
        return await _context.Workshops
            .AsNoTracking()
            .Where(w => w.Recipes.Any(r => r.Id == recipeId))
            .OrderBy(w => w.StartsAt)
            .ThenBy(w => w.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByRecipeAsync(int recipeId, CancellationToken cancellationToken)
    {
        return await _context.Workshops
            .AsNoTracking()
            .AnyAsync(w => w.Recipes.Any(r => r.Id == recipeId), cancellationToken);
    }

    public async Task LinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken)
    {
        var workshop = await _context.Workshops
            .Include(w => w.Recipes)
            .FirstOrDefaultAsync(w => w.Id == workshopId, cancellationToken);
        var recipe = await _context.Recipes.FindAsync(new object[] { recipeId }, cancellationToken);

        if (workshop == null || recipe == null)
            return;

        if (workshop.Recipes.Any(r => r.Id == recipeId))
            return;

        workshop.Recipes.Add(recipe);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken)
    {
        var workshop = await _context.Workshops
            .Include(w => w.Recipes)
            .FirstOrDefaultAsync(w => w.Id == workshopId, cancellationToken);

        var recipe = workshop?.Recipes.FirstOrDefault(r => r.Id == recipeId);
        if (workshop == null || recipe == null)
            return;

        workshop.Recipes.Remove(recipe);
        await _context.SaveChangesAsync(cancellationToken);
    }
}