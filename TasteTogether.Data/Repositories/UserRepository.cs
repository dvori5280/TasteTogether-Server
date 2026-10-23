using Microsoft.EntityFrameworkCore;
using TasteTogether.Core.Entities;
using TasteTogether.Core.Interfaces;

namespace TasteTogether.Data;

public class UserRepository : IUserRepository
{
    private readonly TasteTogetherDbContext _context;

    public UserRepository(TasteTogetherDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Users.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}