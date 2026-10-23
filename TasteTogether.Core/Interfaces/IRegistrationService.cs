using TasteTogether.Core.DTOs;

namespace TasteTogether.Core.Interfaces;

public interface IRegistrationService
{
    Task<RegistrationResponse> RegisterAsync(int workshopId, int userId, PlaceRegistrationRequest request, CancellationToken cancellationToken);
    Task CancelAsync(int workshopId, int userId, CancellationToken cancellationToken);
    Task<List<RegistrationResponse>> GetByUserAsync(int userId, CancellationToken cancellationToken);
    Task<List<RegistrationResponse>> GetByWorkshopAsync(int workshopId, CancellationToken cancellationToken);
}