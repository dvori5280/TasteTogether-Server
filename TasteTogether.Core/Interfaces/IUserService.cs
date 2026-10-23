using TasteTogether.Core.DTOs;

namespace TasteTogether.Core.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
}