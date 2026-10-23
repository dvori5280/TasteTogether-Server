using AutoMapper;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;
using TasteTogether.Core.Enums;

namespace TasteTogether.Service;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IMapper _mapper;
    private const int MaxPageSize = 20;

    public RecipeService(
        IRecipeRepository recipeRepository,
        IWorkshopRepository workshopRepository,
        IMapper mapper)
    {
        _recipeRepository = recipeRepository;
        _workshopRepository = workshopRepository;
        _mapper = mapper;
    }

    public async Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request, int userId, CancellationToken cancellationToken)
    {
        // בונים את הישות ידנית, כמו ב-WorkshopService.CreateAsync.
        // Id ו-CreatedAt מתמלאים אוטומטית, ו-UserId מגיע מה-Token ולא מהבקשה
        var recipe = new Recipe
        {
            Title = request.Title,
            Description = request.Description,
            IngredientsText = request.IngredientsText,
            InstructionsText = request.InstructionsText,
            UserId = userId
        };

        var saved = await _recipeRepository.AddAsync(recipe, cancellationToken);

        return _mapper.Map<RecipeResponse>(saved);
    }

    public async Task<RecipeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, cancellationToken);
        if (recipe == null)
            return null;

        return _mapper.Map<RecipeResponse>(recipe);
    }

    public async Task<PagedResponse<RecipeResponse>> GetAllAsync(
    string? search, RecipeSortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1)
            throw new BadRequestException("מספר העמוד חייב להיות 1 ומעלה");

        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new BadRequestException($"גודל העמוד חייב להיות בין 1 ל-{MaxPageSize}");

        // ASP.NET מקבל גם מספר שאינו ברשימת ה-Enum, למשל 7, ולכן בודקים כאן
        if (!Enum.IsDefined(sortOrder))
            throw new BadRequestException("סוג המיון אינו חוקי");

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var recipes = await _recipeRepository.GetAllAsync(term, sortOrder, page, pageSize, cancellationToken);
        var totalCount = await _recipeRepository.CountAsync(term, cancellationToken);

        return new PagedResponse<RecipeResponse>
        {
            Items = _mapper.Map<List<RecipeResponse>>(recipes),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RecipeResponse> UpdateAsync(
        int id, UpdateRecipeRequest request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("המתכון לא נמצא");

        // החלפה מלאה של שדות התוכן. Id, CreatedAt ו-UserId נשארים כמו שהם
        recipe.Title = request.Title;
        recipe.Description = request.Description;
        recipe.IngredientsText = request.IngredientsText;
        recipe.InstructionsText = request.InstructionsText;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _recipeRepository.UpdateAsync(recipe, cancellationToken);

        return _mapper.Map<RecipeResponse>(recipe);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("המתכון לא נמצא");

        // ולידציה עסקית שתלויה במצב הנתונים, ולכן היא ב-Service ולא ב-Controller
        if (await _workshopRepository.ExistsByRecipeAsync(id, cancellationToken))
            throw new ConflictException("לא ניתן למחוק מתכון שמקושר לסדנה. יש להסיר קודם את הקישור");

        await _recipeRepository.DeleteAsync(recipe, cancellationToken);
    }
}