using TasteTogether.Core.DTOs;
using TasteTogether.Core.Enums;

namespace TasteTogether.Core.Interfaces;

public interface IRecipeService
{
    Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request, int userId, CancellationToken cancellationToken);

    Task<RecipeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PagedResponse<RecipeResponse>> GetAllAsync(
        string? search, RecipeSortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken);

    Task<RecipeResponse> UpdateAsync(
        int id, UpdateRecipeRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}