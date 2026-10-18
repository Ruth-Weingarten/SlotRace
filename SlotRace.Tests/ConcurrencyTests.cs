using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SlotRace.Domain.Entities;
using SlotRace.Domain.Enums;
using SlotRace.Infrastructure.Persistence;

namespace SlotRace.Tests;

// ההוכחה (T24, דרישה ג' של המורה): מסד אמיתי, בלי Mock.
// שני DbContext נפרדים מדמים שתי בקשות במקביל (בשרת כל בקשה מקבלת DbContext משלה, Scoped)
public class ConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ConcurrencyTests()
    {
        // SQLite בזיכרון: המסד חי כל עוד החיבור פתוח. לכל בדיקה מסד חדש ונקי
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        // EnsureCreated ולא migrations: ה-migrations נכתבו ל-PostgreSQL.
        // כאן הטבלאות נבנות ישירות מ-AppDbContext, כולל Version והאינדקס הייחודי
        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();   // סגירת החיבור מוחקת את המסד

    private AppDbContext NewContext() => new(_options);

    // נתונים: מחנכת, שלוש הורות, שתי ילדות, שני סלוטים פנויים
    private async Task<SeedData> SeedAsync()
    {
        await using var context = NewContext();

        var teacher = new User { FullName = "שרה כהן",   Email = "sara@t.test",   PasswordHash = "x", Role = Role.Teacher };
        var leah    = new User { FullName = "לאה אברהם", Email = "leah@t.test",   PasswordHash = "x", Role = Role.Parent };
        var david   = new User { FullName = "דוד אברהם", Email = "david@t.test",  PasswordHash = "x", Role = Role.Parent };
        var rachel  = new User { FullName = "רחל גולד",  Email = "rachel@t.test", PasswordHash = "x", Role = Role.Parent };

        var miri = new Student { FullName = "מירי אברהם", JoinCode = "AAAA2222", HomeroomTeacher = teacher, Parents = { leah, david } };
        var hani = new Student { FullName = "חני גולד",   JoinCode = "BBBB3333", HomeroomTeacher = teacher, Parents = { rachel } };

        var start = new DateTime(2026, 10, 25, 14, 0, 0, DateTimeKind.Utc);
        var slot1 = new Slot { Teacher = teacher, StartTimeUtc = start,                EndTimeUtc = start.AddMinutes(15) };
        var slot2 = new Slot { Teacher = teacher, StartTimeUtc = start.AddMinutes(15), EndTimeUtc = start.AddMinutes(30) };

        context.AddRange(teacher, leah, david, rachel, miri, hani, slot1, slot2);
        await context.SaveChangesAsync();

        return new SeedData(slot1.Id, slot2.Id, leah.Id, david.Id, rachel.Id, miri.Id, hani.Id);
    }

    private record SeedData(int Slot1Id, int Slot2Id, int LeahId, int DavidId, int RachelId, int MiriId, int HaniId);

    [Fact]
    public async Task TwoParentsBookSameSlot_SecondSaveThrowsConcurrencyException()
    {
        var data = await SeedAsync();
        await using var leahContext = NewContext();     // "הבקשה של לאה"
        await using var rachelContext = NewContext();   // "הבקשה של רחל"

        // שתיהן קוראות את אותו סלוט, ושתיהן רואות אותו פנוי (עם אותו Version)
        var slotForLeah = await leahContext.Slots.SingleAsync(s => s.Id == data.Slot1Id);
        var slotForRachel = await rachelContext.Slots.SingleAsync(s => s.Id == data.Slot1Id);
        Assert.Null(slotForLeah.BookedByParentId);
        Assert.Null(slotForRachel.BookedByParentId);

        slotForLeah.BookedByParentId = data.LeahId;
        slotForLeah.StudentId = data.MiriId;
        slotForRachel.BookedByParentId = data.RachelId;
        slotForRachel.StudentId = data.HaniId;

        // לאה שומרת ראשונה: מצליח, וה-Version מתחלף (ה-override ב-AppDbContext)
        await leahContext.SaveChangesAsync();

        // רחל שומרת עם ה-Version הישן: UPDATE ... WHERE Version=@old → 0 שורות → חריגה
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => rachelContext.SaveChangesAsync());

        // context שלישי בודק מה באמת נשמר: הזמנה אחת בלבד, של לאה
        await using var check = NewContext();
        var slot = await check.Slots.SingleAsync(s => s.Id == data.Slot1Id);
        Assert.Equal(data.LeahId, slot.BookedByParentId);
        Assert.Equal(1, await check.Slots.CountAsync(s => s.BookedByParentId != null));
    }

    [Fact]
    public async Task TwoParentsBookDifferentSlotsForSameStudent_SecondSaveThrows()
    {
        var data = await SeedAsync();
        await using var davidContext = NewContext();    // "הבקשה של האבא"
        await using var leahContext = NewContext();     // "הבקשה של האמא"

        // שני סלוטים שונים, אותה ילדה. Version לא עוזר כאן: לכל סלוט Version משלו
        var slotForDavid = await davidContext.Slots.SingleAsync(s => s.Id == data.Slot1Id);
        var slotForLeah = await leahContext.Slots.SingleAsync(s => s.Id == data.Slot2Id);

        slotForDavid.BookedByParentId = data.DavidId;
        slotForDavid.StudentId = data.MiriId;
        slotForLeah.BookedByParentId = data.LeahId;
        slotForLeah.StudentId = data.MiriId;

        await davidContext.SaveChangesAsync();

        // האינדקס הייחודי על StudentId (T22) דוחה את השמירה השנייה.
        // ב-SQLite אין PostgresException, ולכן בודקים את החריגה הכללית של EF
        await Assert.ThrowsAsync<DbUpdateException>(() => leahContext.SaveChangesAsync());

        // במסד: למירי תור אחד בלבד
        await using var check = NewContext();
        Assert.Equal(1, await check.Slots.CountAsync(s => s.StudentId == data.MiriId));
    }
}
