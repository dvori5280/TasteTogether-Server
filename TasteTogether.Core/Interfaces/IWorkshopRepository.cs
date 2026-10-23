using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;

namespace TasteTogether.Core.Interfaces;

public interface IWorkshopRepository
{
    Task<Workshop?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Workshop>> GetAllAsync(
        WorkshopStatus? status, DateTime? startsFrom, int? recipeId, DateTime now, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<int> CountAsync(
        WorkshopStatus? status, DateTime? startsFrom, int? recipeId, CancellationToken cancellationToken);
    Task<Workshop> AddAsync(Workshop workshop, CancellationToken cancellationToken);
    Task UpdateAsync(Workshop workshop, CancellationToken cancellationToken);
    Task<List<Workshop>> GetByRecipeAsync(int recipeId, CancellationToken cancellationToken);
    Task LinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken);
    Task UnlinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken);
    Task<bool> ExistsByRecipeAsync(int recipeId, CancellationToken cancellationToken);
}