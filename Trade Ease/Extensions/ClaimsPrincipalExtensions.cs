using System.Security.Claims;

namespace TradeEase;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    public static string GetRoleName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? "Trader";

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.GetRoleName() == "Admin";
}
