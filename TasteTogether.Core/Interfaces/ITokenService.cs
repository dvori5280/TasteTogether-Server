using TasteTogether.Core.DTOs;

namespace TasteTogether.Core.Interfaces;

public interface ITokenService
{
    LoginResponse CreateToken(UserResponse user);
}