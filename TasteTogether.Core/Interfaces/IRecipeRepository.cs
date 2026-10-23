using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;

namespace TasteTogether.Core.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Recipe>> GetByWorkshopAsync(int workshopId, CancellationToken cancellationToken);
    Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken);
    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken);
    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken);
    Task<List<Recipe>> GetAllAsync(
        string? search, RecipeSortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, CancellationToken cancellationToken);
}