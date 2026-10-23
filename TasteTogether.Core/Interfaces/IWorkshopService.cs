using TasteTogether.Core.DTOs;
using TasteTogether.Core.Enums;

namespace TasteTogether.Core.Interfaces;

public interface IWorkshopService
{
    Task<PagedResponse<WorkshopResponse>> GetAllAsync(
        WorkshopStatus? status, int? recipeId, bool isAdmin, int page, int pageSize, CancellationToken cancellationToken);
    Task<WorkshopResponse?> GetByIdAsync(int id, bool isAdmin, CancellationToken cancellationToken);
    Task<List<RecipeResponse>> GetRecipesAsync(int workshopId, bool isAdmin, CancellationToken cancellationToken);
    Task<WorkshopResponse> CreateAsync(CreateWorkshopRequest request, int hostUserId, CancellationToken cancellationToken);
    Task<WorkshopResponse> UpdateStatusAsync(int id, UpdateWorkshopStatusRequest request, CancellationToken cancellationToken);
    Task LinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken);
    Task UnlinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken);
}