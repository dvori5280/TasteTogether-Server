using AutoMapper;
using Microsoft.Extensions.Logging;
using TasteTogether.Core.DTOs;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Enums;
using TasteTogether.Core.Exceptions;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.Service;

public class RegistrationService : IRegistrationService
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IRegistrationRepository registrationRepository,
        IWorkshopRepository workshopRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<RegistrationService> logger)
    {
        _registrationRepository = registrationRepository;
        _workshopRepository = workshopRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RegistrationResponse> RegisterAsync(
        int workshopId, int userId, PlaceRegistrationRequest request, CancellationToken cancellationToken)
    {
        // חדש: ה-[Required] ב-DTO כבר דוחה בקשה בלי Version.
        // הבדיקה כאן מגינה על המקרה שה-Service נקרא ישירות, למשל מבדיקה
        var requestedVersion = request.Version
            ?? throw new BadRequestException("יש לשלוח את ה-Version של הסדנה");

        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken)
            ?? throw new NotFoundException("הסדנה לא נמצאה");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("המשתמש לא נמצא");

        // טיוטה גלויה רק לאדמינית (L6), ולכן כאן היא מתנהגת כאילו לא קיימת
        if (workshop.Status == WorkshopStatus.Draft)
            throw new NotFoundException("הסדנה לא נמצאה");

        if (workshop.Status == WorkshopStatus.Cancelled)
            throw new BadRequestException("הסדנה בוטלה");

        if (workshop.Status != WorkshopStatus.Published)
            throw new BadRequestException("הסדנה אינה פתוחה להרשמה");

        if (workshop.StartsAt <= DateTime.UtcNow)
            throw new BadRequestException("הסדנה כבר התחילה");

        if (await _registrationRepository.GetByUserAndWorkshopAsync(userId, workshopId, cancellationToken) != null)
            throw new ConflictException("כבר נרשמת לסדנה זו");

        if (await _registrationRepository.CountByWorkshopAsync(workshopId, cancellationToken) >= workshop.Capacity)
            throw new ConflictException("הסדנה מלאה");

        // חדש: הלקוחה ראתה גרסה ישנה של הסדנה (מישהי נרשמה או שהסדנה השתנתה מאז)
        if (requestedVersion != workshop.Version)
        {
            // N5: התנגשות Concurrency שנתפסה ב-Service. רושמים רק מזהים
            _logger.LogWarning(
                "Stale workshop version: user {UserId} tried to register to workshop {WorkshopId} with an outdated version",
                userId, workshopId);

            throw new ConflictException("הסדנה השתנתה מאז שהצגת אותה. רענני ונסי שוב");
        }

        var registration = new WorkshopRegistration
        {
            UserId = userId,
            WorkshopId = workshopId
        };

        // חדש: שמירה אחת שכוללת את ההרשמה והחלפת ה-Version של הסדנה.
        // מירוץ אמיתי בין שתי בקשות ייתפס כאן ב-WHERE של ה-UPDATE (הטיפול ב-409 ייכנס ב-M5)
        var saved = await _registrationRepository.AddAsync(registration, workshop, cancellationToken);

        // N4: אירוע עסקי. נרשם רק אחרי שהשמירה הצליחה, ובמזהים בלבד
        _logger.LogInformation("User {UserId} registered to workshop {WorkshopId}", userId, workshopId);

        return ToResponse(saved, workshop.Title, workshop.Status, user.Email, user.FullName);
    }

    public async Task CancelAsync(int workshopId, int userId, CancellationToken cancellationToken)
    {
        var registration = await _registrationRepository.GetByUserAndWorkshopAsync(userId, workshopId, cancellationToken)
            ?? throw new NotFoundException("ההרשמה לא נמצאה");

        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken)
            ?? throw new NotFoundException("הסדנה לא נמצאה");

        if (workshop.StartsAt <= DateTime.UtcNow)
            throw new BadRequestException("לא ניתן לבטל הרשמה לאחר תחילת הסדנה");

        await _registrationRepository.DeleteAsync(registration, cancellationToken);

        // N4: אירוע עסקי. נרשם רק אחרי שהמחיקה הצליחה, ובמזהים בלבד
        _logger.LogInformation("User {UserId} cancelled registration to workshop {WorkshopId}", userId, workshopId);
    }

    public async Task<List<RegistrationResponse>> GetByUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("המשתמש לא נמצא");

        var registrations = await _registrationRepository.GetByUserAsync(userId, cancellationToken);

        return registrations
            .Select(r => ToResponse(r, r.Workshop.Title, r.Workshop.Status, user.Email, user.FullName))
            .ToList();
    }

    public async Task<List<RegistrationResponse>> GetByWorkshopAsync(int workshopId, CancellationToken cancellationToken)
    {
        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken)
            ?? throw new NotFoundException("הסדנה לא נמצאה");

        var registrations = await _registrationRepository.GetByWorkshopAsync(workshopId, cancellationToken);

        return registrations
            .Select(r => ToResponse(r, workshop.Title, workshop.Status, r.User.Email, r.User.FullName))
            .ToList();
    }

    // AutoMapper ממפה את שדות ההרשמה. הכותרת, הסטטוס, האימייל והשם מוזנים כאן כי הניווטים לא תמיד נטענים
    private RegistrationResponse ToResponse(
        WorkshopRegistration registration,
        string workshopTitle,
        WorkshopStatus workshopStatus,
        string userEmail,
        string userFullName)
    {
        var response = _mapper.Map<RegistrationResponse>(registration);
        response.WorkshopTitle = workshopTitle;
        response.WorkshopStatus = workshopStatus.ToString();
        response.UserEmail = userEmail;
        response.UserFullName = userFullName;
        return response;
    }
}