using System.Security.Claims;

namespace MedicAI.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");

        return int.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Invalid user identifier claim.");
    }

    public static string GetRoleName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
