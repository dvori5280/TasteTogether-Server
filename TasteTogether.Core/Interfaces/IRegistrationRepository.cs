using TasteTogether.Core.Entities;

namespace TasteTogether.Core.Interfaces;

public interface IRegistrationRepository
{
    Task<WorkshopRegistration?> GetByUserAndWorkshopAsync(int userId, int workshopId, CancellationToken cancellationToken);
    Task<List<WorkshopRegistration>> GetByUserAsync(int userId, CancellationToken cancellationToken);
    Task<List<WorkshopRegistration>> GetByWorkshopAsync(int workshopId, CancellationToken cancellationToken);
    Task<int> CountByWorkshopAsync(int workshopId, CancellationToken cancellationToken);
    Task<Dictionary<int, int>> CountByWorkshopsAsync(List<int> workshopIds, CancellationToken cancellationToken);
    Task<WorkshopRegistration> AddAsync(WorkshopRegistration registration, Workshop workshop, CancellationToken cancellationToken);
    Task DeleteAsync(WorkshopRegistration registration, CancellationToken cancellationToken);
}