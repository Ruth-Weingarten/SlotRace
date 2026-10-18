using Microsoft.EntityFrameworkCore;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Domain.Enums;

namespace SlotRace.Infrastructure.Persistence;

// רץ בעליית השרת: מעדכן את המסד לגרסה האחרונה, ואם הוא ריק, ממלא נתוני דוגמה.
// כך "מוחקים את המסד, מריצים, והכול עולה" בלי שום פקודה ידנית (דרישת המורה)
public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher, TimeProvider time, CancellationToken ct = default)
    {
        // 1. מבצע את כל המיגרציות שעוד לא בוצעו (לפי הטבלה __EFMigrationsHistory)
        await context.Database.MigrateAsync(ct);

        // 2. יש כבר נתונים? לא נוגעים, כדי לא ליצור כפילויות
        if (await context.Users.AnyAsync(ct))
            return;

        var hash = hasher.Hash(DemoPassword);   // hash אחד לכולם (אותה סיסמה)

        // מחנכות: שתיים, כדי להדגים 403 אצל מחנכת אחרת
        var sara  = new User { FullName = "שרה כהן",  Email = "sara@slotrace.test",  PasswordHash = hash, Role = Role.Teacher };
        var rivka = new User { FullName = "רבקה לוי", Email = "rivka@slotrace.test", PasswordHash = hash, Role = Role.Teacher };

        // הורים: לאה ודוד הם הורים של מירי (הדגמת "תור אחד לילדה"); רחל כבר הזמינה תור
        var leah   = new User { FullName = "לאה אברהם", Email = "leah@slotrace.test",   PasswordHash = hash, Role = Role.Parent };
        var david  = new User { FullName = "דוד אברהם", Email = "david@slotrace.test",  PasswordHash = hash, Role = Role.Parent };
        var rachel = new User { FullName = "רחל גולד",  Email = "rachel@slotrace.test", PasswordHash = hash, Role = Role.Parent };

        // אובייקטים ולא מספרים (HomeroomTeacher = sara): עוד אין Id-ים.
        // EF מכניס קודם את המשתמשות, ואז את התלמידות עם ה-Id-ים הנכונים
        var miri  = new Student { FullName = "מירי אברהם", JoinCode = "SARA2345", HomeroomTeacher = sara,  Parents = { leah, david } };
        var noa   = new Student { FullName = "נועה אברהם", JoinCode = "RVKA2345", HomeroomTeacher = rivka, Parents = { leah } };
        var hani  = new Student { FullName = "חני גולד",   JoinCode = "SARA6789", HomeroomTeacher = sara,  Parents = { rachel } };
        var tamar = new Student { FullName = "תמר כץ",     JoinCode = "RVKA6789", HomeroomTeacher = rivka };   // בלי הורים: להדגמת קישור לפי קוד

        // סלוטים: מחר מ-14:00 UTC (17:00 בישראל), רבע שעה כל אחד, 6 לכל מחנכת.
        // מחושב ממועד ההרצה, ולכן תמיד בעתיד
        var firstStart = time.GetUtcNow().UtcDateTime.Date.AddDays(1).AddHours(14);
        var slots = new List<Slot>();
        foreach (var teacher in new[] { sara, rivka })
        {
            for (var i = 0; i < 6; i++)
            {
                var start = firstStart.AddMinutes(15 * i);
                slots.Add(new Slot { Teacher = teacher, StartTimeUtc = start, EndTimeUtc = start.AddMinutes(15) });
            }
        }

        // הסלוט הראשון של שרה כבר מוזמן: רחל עבור חני (הדגמת "תפוס" ופרטיות)
        slots[0].BookedByParent = rachel;
        slots[0].Student = hani;

        context.Users.AddRange(sara, rivka, leah, david, rachel);
        context.Students.AddRange(miri, noa, hani, tamar);
        context.Slots.AddRange(slots);

        await context.SaveChangesAsync(ct);   // הכול בשמירה אחת
    }
}
