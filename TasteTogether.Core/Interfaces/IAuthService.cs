using TasteTogether.Core.DTOs;

namespace TasteTogether.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}