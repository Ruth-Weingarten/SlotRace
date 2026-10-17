using SlotRace.Domain.Entities;

namespace SlotRace.Application.Interfaces;

// יצירת token למשתמשת. המימוש ב-Infrastructure, כדי ש-Application לא תכיר JWT.
public interface IJwtTokenGenerator
{
    // tuple: מחזיר שני ערכים, ה-token עצמו ומתי הוא פג (שניהם נכנסים ל-AuthResponse)
    (string Token, DateTime ExpiresAtUtc) Generate(User user);
}
