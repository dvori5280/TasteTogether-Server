using AutoMapper;
using Microsoft.Extensions.Logging;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;
using TasteTogether.Core.Enums;

namespace TasteTogether.Service;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<WorkshopService> _logger;
    private const int MaxPageSize = 20;
    private const int PastWindowDays = 30;

    public WorkshopService(
        IWorkshopRepository workshopRepository,
        IRegistrationRepository registrationRepository,
        IRecipeRepository recipeRepository,
        IMapper mapper,
        ILogger<WorkshopService> logger)
    {
        _workshopRepository = workshopRepository;
        _registrationRepository = registrationRepository;
        _recipeRepository = recipeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResponse<WorkshopResponse>> GetAllAsync(
     WorkshopStatus? status, int? recipeId, bool isAdmin, int page, int pageSize,
     CancellationToken cancellationToken)
    {
        if (page < 1)
            throw new BadRequestException("מספר העמוד חייב להיות 1 ומעלה");

        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new BadRequestException($"גודל העמוד חייב להיות בין 1 ל-{MaxPageSize}");

        // ASP.NET מקבל גם מספר שאינו ברשימת ה-Enum, למשל 7
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new BadRequestException("הסטטוס אינו חוקי");

        // סינון לפי מתכון שלא קיים הוא 404, ולא רשימה ריקה
        if (recipeId.HasValue
            && await _recipeRepository.GetByIdAsync(recipeId.Value, cancellationToken) == null)
            throw new NotFoundException("המתכון לא נמצא");

        var now = DateTime.UtcNow;

        // אדמינית רואה הכול ויכולה לסנן לפי סטטוס.
        // אחרות רואות רק Published, ורק סדנאות שמועדן מ-30 הימים האחרונים והלאה
        WorkshopStatus? statusFilter = isAdmin ? status : WorkshopStatus.Published;
        DateTime? startsFrom = isAdmin ? null : now.AddDays(-PastWindowDays);

        var workshops = await _workshopRepository.GetAllAsync(
            statusFilter, startsFrom, recipeId, now, page, pageSize, cancellationToken);
        var totalCount = await _workshopRepository.CountAsync(
            statusFilter, startsFrom, recipeId, cancellationToken);

        // שאילתה אחת לספירת הנרשמים של כל הסדנאות ברשימה (בלי N+1)
        var ids = workshops.Select(w => w.Id).ToList();
        var counts = await _registrationRepository.CountByWorkshopsAsync(ids, cancellationToken);

        return new PagedResponse<WorkshopResponse>
        {
            Items = workshops.Select(w => ToResponse(w, counts.GetValueOrDefault(w.Id))).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }



    public async Task<WorkshopResponse?> GetByIdAsync(
        int id, bool isAdmin, CancellationToken cancellationToken)
    {
        var workshop = await _workshopRepository.GetByIdAsync(id, cancellationToken);
        if (workshop == null)
            return null;

        // טיוטה גלויה רק לאדמינית. לאחרות היא מתנהגת כאילו לא קיימת
        if (workshop.Status == WorkshopStatus.Draft && !isAdmin)
            return null;

        var count = await _registrationRepository.CountByWorkshopAsync(id, cancellationToken);
        return ToResponse(workshop, count);
    }

    public async Task<List<RecipeResponse>> GetRecipesAsync(
       int workshopId, bool isAdmin, CancellationToken cancellationToken)
    {
        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken);

        // אותה נראות כמו בפרטי הסדנה: טיוטה גלויה רק לאדמינית
        if (workshop == null || (workshop.Status == WorkshopStatus.Draft && !isAdmin))
            throw new NotFoundException("הסדנה לא נמצאה");

        var recipes = await _recipeRepository.GetByWorkshopAsync(workshopId, cancellationToken);
        return _mapper.Map<List<RecipeResponse>>(recipes);
    }

    public async Task<WorkshopResponse> CreateAsync(
    CreateWorkshopRequest request, int hostUserId, CancellationToken cancellationToken)
    {
        // ה-[Required] ב-DTO כבר דוחה בקשה בלי מועד.
        // הבדיקה כאן מגינה על המקרה שה-Service נקרא ישירות, למשל מבדיקה
        var startsAt = request.StartsAt
            ?? throw new BadRequestException("יש לשלוח את מועד הסדנה");

        if (startsAt.Kind != DateTimeKind.Utc)
            throw new BadRequestException(
                "יש לשלוח את המועד בזמן UTC, למשל 2026-12-01T18:00:00Z");

        if (startsAt <= DateTime.UtcNow)
            throw new BadRequestException("מועד הסדנה חייב להיות בעתיד");

        var workshop = new Workshop
        {
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartsAt = startsAt,
            Capacity = request.Capacity,
            HostId = hostUserId
        };

        var saved = await _workshopRepository.AddAsync(workshop, cancellationToken);

        // סדנה חדשה, ולכן אין לה נרשמים
        return ToResponse(saved, 0);
    }

    public async Task<WorkshopResponse> UpdateStatusAsync(
    int id, UpdateWorkshopStatusRequest request, CancellationToken cancellationToken)
    {
        // המרה ממחרוזת ל-Enum. IsDefined חוסם גם מספר שאינו ברשימה, למשל "7"
        if (!Enum.TryParse<WorkshopStatus>(request.Status, ignoreCase: true, out var newStatus)
            || !Enum.IsDefined(newStatus))
            throw new BadRequestException("הסטטוס אינו חוקי");

        // מותר לעבור רק ל-Published או ל-Cancelled. אי אפשר לחזור לטיוטה
        if (newStatus == WorkshopStatus.Draft)
            throw new BadRequestException("לא ניתן להחזיר סדנה לטיוטה");

        var workshop = await _workshopRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("הסדנה לא נמצאה");

        if (newStatus == WorkshopStatus.Published)
            EnsureCanPublish(workshop);
        else
            EnsureCanCancel(workshop);

        workshop.Status = newStatus;
        await _workshopRepository.UpdateAsync(workshop, cancellationToken);

        // N4: אירוע עסקי (פרסום או ביטול סדנה). נרשם רק אחרי שהעדכון הצליח, ובמזהים בלבד
        _logger.LogInformation("Workshop {WorkshopId} status changed to {Status}", id, newStatus);

        var count = await _registrationRepository.CountByWorkshopAsync(id, cancellationToken);
        return ToResponse(workshop, count);
    }

    // מפרסמים רק סדנה שנמצאת בטיוטה ושעוד לא התחילה
    private static void EnsureCanPublish(Workshop workshop)
    {
        if (workshop.Status == WorkshopStatus.Published)
            throw new ConflictException("הסדנה כבר פורסמה");

        if (workshop.Status == WorkshopStatus.Cancelled)
            throw new ConflictException("לא ניתן לפרסם סדנה שבוטלה");

        if (workshop.StartsAt <= DateTime.UtcNow)
            throw new BadRequestException("לא ניתן לפרסם סדנה שמועדה כבר עבר");
    }

    // מבטלים סדנה מטיוטה או מפורסמת, כל עוד לא בוטלה ולא התחילה. ההרשמות הקיימות נשארות
    private static void EnsureCanCancel(Workshop workshop)
    {
        if (workshop.Status == WorkshopStatus.Cancelled)
            throw new ConflictException("הסדנה כבר בוטלה");

        if (workshop.StartsAt <= DateTime.UtcNow)
            throw new BadRequestException("לא ניתן לבטל סדנה שכבר התחילה");
    }

    public async Task LinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken)
    {
        await EnsureWorkshopAndRecipeExistAsync(workshopId, recipeId, cancellationToken);
        await _workshopRepository.LinkRecipeAsync(workshopId, recipeId, cancellationToken);
    }

    public async Task UnlinkRecipeAsync(int workshopId, int recipeId, CancellationToken cancellationToken)
    {
        await EnsureWorkshopAndRecipeExistAsync(workshopId, recipeId, cancellationToken);
        await _workshopRepository.UnlinkRecipeAsync(workshopId, recipeId, cancellationToken);
    }

    // ה-Repository מתעלם בשקט מסדנה או מתכון שלא קיימים, ולכן הבדיקה כאן
    private async Task EnsureWorkshopAndRecipeExistAsync(
        int workshopId, int recipeId, CancellationToken cancellationToken)
    {
        if (await _workshopRepository.GetByIdAsync(workshopId, cancellationToken) == null)
            throw new NotFoundException("הסדנה לא נמצאה");

        if (await _recipeRepository.GetByIdAsync(recipeId, cancellationToken) == null)
            throw new NotFoundException("המתכון לא נמצא");
    }

    // AutoMapper ממפה את שדות הישות. RegisteredCount ו-IsPast מחושבים מחוץ לישות ולכן מוזנים כאן
    private WorkshopResponse ToResponse(Workshop workshop, int registeredCount)
    {
        var response = _mapper.Map<WorkshopResponse>(workshop);
        response.RegisteredCount = registeredCount;
        response.IsPast = workshop.StartsAt <= DateTime.UtcNow;
        return response;
    }
}