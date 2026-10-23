using Microsoft.EntityFrameworkCore;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Interfaces;
using TasteTogether.Core.Enums;

namespace TasteTogether.Data;

public class RecipeRepository : IRecipeRepository
{
    private readonly TasteTogetherDbContext _context;

    public RecipeRepository(TasteTogetherDbContext context)
    {
        _context = context;
    }

    public async Task<Recipe?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Recipe>> GetByWorkshopAsync(int workshopId, CancellationToken cancellationToken)
    {
        return await _context.Recipes
            .AsNoTracking()
            .Where(r => r.Workshops.Any(w => w.Id == workshopId))
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync(cancellationToken);
        return recipe;
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        // הישות נטענה בלי מעקב, ולכן Update אומר ל-EF שהיא קיימת ושונתה
        _context.Recipes.Update(recipe);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Recipe>> GetAllAsync(
    string? search, RecipeSortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = ApplySearch(_context.Recipes.AsNoTracking(), search);

        // Id בסוף כל מיון מבטיח סדר יציב גם כשיש ערכים זהים, ולכן הדפדוף לא קופץ בין עמודים
        query = sortOrder switch
        {
            RecipeSortOrder.Oldest => query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
            RecipeSortOrder.Title => query.OrderBy(r => r.Title).ThenBy(r => r.Id),
            _ => query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
        };

        // הסינון והחיתוך נעשים במסד הנתונים, לפני ToListAsync
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string? search, CancellationToken cancellationToken)
    {
        return await ApplySearch(_context.Recipes.AsNoTracking(), search)
            .CountAsync(cancellationToken);
    }

    // אותו סינון גם לרשימה וגם לספירה, כדי שהסך הכול יתאים תמיד לתוצאות
    private static IQueryable<Recipe> ApplySearch(IQueryable<Recipe> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.ToLower();
        return query.Where(r => r.Title.ToLower().Contains(term));
    }
}