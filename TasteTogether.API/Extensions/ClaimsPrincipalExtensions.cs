using System.Security.Claims;

namespace TasteTogether.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("User id claim is missing or invalid.");
        }

        return userId;
    }
}