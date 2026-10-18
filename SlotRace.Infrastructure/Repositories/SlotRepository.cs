using Microsoft.EntityFrameworkCore;
using Npgsql;
using SlotRace.Application.Common;
using SlotRace.Application.Common.Exceptions;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Infrastructure.Persistence;

namespace SlotRace.Infrastructure.Repositories;

public class SlotRepository : ISlotRepository
{
    private readonly AppDbContext _context;

    public SlotRepository(AppDbContext context)
    {
        _context = context;
    }

    // עם מעקב: EF "זוכר" את הסלוט, ולכן SaveChanges יודע מה השתנה, ו-Version מתחלף.
    // Include כדי שאחרי השינוי אפשר יהיה להחזיר SlotDto עם שם המחנכת והילדה
    public async Task<Slot?> GetForUpdateAsync(int id, CancellationToken ct)
    {
        return await _context.Slots
            .Include(s => s.Teacher)
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    // קריאה בלבד: AsNoTracking מהיר יותר, ו-EF לא "זוכר" כלום
    public async Task<Slot?> GetDetailsAsync(int id, CancellationToken ct)
    {
        return await _context.Slots
            .AsNoTracking()
            .Include(s => s.Teacher)
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<PagedResult<Slot>> GetPagedAsync(SlotQuery query, CancellationToken ct)
    {
        // IQueryable: עדיין לא נשלחה שאילתה. כל Where רק מוסיף תנאי
        IQueryable<Slot> slots = _context.Slots.AsNoTracking();

        if (query.TeacherId.HasValue)
            slots = slots.Where(s => s.TeacherId == query.TeacherId.Value);

        if (query.OnlyFree)
            slots = slots.Where(s => s.BookedByParentId == null);

        if (query.FromUtc.HasValue)
            slots = slots.Where(s => s.StartTimeUtc >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            slots = slots.Where(s => s.StartTimeUtc <= query.ToUtc.Value);

        // שאילתה 1: כמה תוצאות יש בסך הכול, לפני החיתוך לעמודים
        var totalCount = await slots.CountAsync(ct);

        // ThenBy(Id): סדר קבוע גם כששני סלוטים מתחילים באותה שעה, כדי שה-paging יהיה יציב
        slots = query.SortDesc
            ? slots.OrderByDescending(s => s.StartTimeUtc).ThenByDescending(s => s.Id)
            : slots.OrderBy(s => s.StartTimeUtc).ThenBy(s => s.Id);

        // שאילתה 2: רק העמוד המבוקש. Skip/Take לפני ToListAsync, ולכן הם חלק מה-SQL (OFFSET/LIMIT)
        var items = await slots
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(s => s.Teacher)
            .Include(s => s.Student)
            .ToListAsync(ct);

        return new PagedResult<Slot>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    // שני טווחים חופפים אם ורק אם כל אחד מתחיל לפני שהשני נגמר.
    // סלוט שמתחיל בדיוק כשהקודם נגמר (10:15) מותר
    public async Task<bool> HasOverlapAsync(int teacherId, DateTime startUtc, DateTime endUtc,
                                            int? excludeSlotId, CancellationToken ct)
    {
        return await _context.Slots.AnyAsync(s =>
            s.TeacherId == teacherId
            && (excludeSlotId == null || s.Id != excludeSlotId)   // בעדכון: לא להשוות את הסלוט לעצמו
            && s.StartTimeUtc < endUtc
            && startUtc < s.EndTimeUtc,
            ct);
    }

    public async Task<bool> StudentHasBookingAsync(int studentId, CancellationToken ct)
    {
        return await _context.Slots.AnyAsync(s => s.StudentId == studentId, ct);
    }

    public async Task AddAsync(Slot slot, CancellationToken ct)
    {
        await _context.Slots.AddAsync(slot, ct);
    }

    // לא async: רק מסמן למחיקה. ה-DELETE נשלח ב-SaveChangesAsync
    public void Remove(Slot slot)
    {
        _context.Slots.Remove(slot);
    }

    // מתרגם את חריגות EF ל-SlotConflictException, כדי ש-Application לא תכיר את EF
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        // מישהי אחרת שינתה את הסלוט מאז שקראנו אותו: ה-Version לא תאם, ו-0 שורות עודכנו.
        // חייב להיות לפני DbUpdateException, כי הוא יורש ממנו
        catch (DbUpdateConcurrencyException ex)
        {
            throw new SlotConflictException("הסלוט שונה על ידי בקשה אחרת.", ex);
        }
        // when: תופסים רק הפרת אינדקס ייחודי (23505). כל שגיאה אחרת ממשיכה הלאה ל-500
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new SlotConflictException("ההזמנה מתנגשת בהזמנה קיימת.", ex);
        }
    }
}
