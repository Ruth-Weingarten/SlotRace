using System.Security.Claims;
using SlotRace.Domain.Enums;

namespace SlotRace.API.Extensions;

// מתודות הרחבה: מאפשרות לכתוב User.GetUserId() ב-Controller.
// המשתמשת הנוכחית נקראת תמיד מה-claims של ה-token, אף פעם לא מגוף הבקשה.
// יושב ב-API, כי ClaimsPrincipal הוא עניין של HTTP. השירותים מקבלים int ו-Role רגילים.
public static class ClaimsPrincipalExtensions
{
    // המזהה של המשתמשת המחוברת, מה-claim שנכתב ב-JwtTokenGenerator (T12)
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("אין מזהה משתמשת ב-token.");
        return int.Parse(value);
    }

    // ה-role כ-enum ולא כמחרוזת
    public static Role GetRole(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("אין role ב-token.");
        return Enum.Parse<Role>(value);
    }
}
