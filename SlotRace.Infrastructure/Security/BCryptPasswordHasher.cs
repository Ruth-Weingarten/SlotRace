using SlotRace.Application.Interfaces;

namespace SlotRace.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    // BCrypt מגריל salt ושומר אותו בתוך מחרוזת ה-hash, ולכן אין צורך בעמודה נפרדת
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    // מוציא את ה-salt מה-hash השמור, מחשב את ה-hash של הסיסמה שהוקלדה עם אותו salt, ומשווה
    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
