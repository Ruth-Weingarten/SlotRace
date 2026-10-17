namespace SlotRace.Application.Interfaces;

// הצפנת סיסמאות. המימוש ב-Infrastructure (BCrypt), כדי ש-Application לא תכיר את החבילה.
// אין פעולת "פענוח": hash הוא חד-כיווני. בודקים על ידי חישוב מחדש והשוואה.
public interface IPasswordHasher
{
    string Hash(string password);                       // הרשמה: סיסמה → hash
    bool Verify(string password, string passwordHash);  // התחברות: האם הסיסמה מתאימה ל-hash השמור?
}
