using TasteTogether.Core.Entities;

namespace TasteTogether.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
}






