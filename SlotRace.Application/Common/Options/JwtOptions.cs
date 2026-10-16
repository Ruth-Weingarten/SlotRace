namespace SlotRace.Application.Common.Options;

// ההגדרות של ה-JWT, מתוך החלק "Jwt" בקונפיגורציה.
// Issuer, Audience ו-ExpiryMinutes ב-appsettings.json; Key רק ב-User Secrets.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
}
