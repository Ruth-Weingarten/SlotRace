namespace SlotRace.Application.Common;

// סוגי הכשלונות הצפויים של פעולה עסקית.
// ה-Controller מתרגם כל סוג לסטטוס HTTP (ב-T19).
public enum ErrorType
{
    None,          // אין שגיאה: הפעולה הצליחה
    Validation,    // 400: הנתונים לא תקינים (למשל סלוט בעבר)
    Unauthorized,  // 401: אימייל או סיסמה שגויים
    Forbidden,     // 403: מחוברת, אבל אין לה הרשאה לפעולה הזאת
    NotFound,      // 404: הדבר המבוקש לא קיים
    Conflict       // 409: התנגשות (סלוט תפוס, אימייל קיים)
}
