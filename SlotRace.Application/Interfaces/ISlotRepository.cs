using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Domain.Entities;

namespace SlotRace.Application.Interfaces;

public interface ISlotRepository
{
    // לשינוי: עם מעקב (בלי AsNoTracking), כדי ש-SaveChanges ו-Version יעבדו
    Task<Slot?> GetForUpdateAsync(int id, CancellationToken ct);

    // לקריאה בלבד: AsNoTracking
    Task<Slot?> GetDetailsAsync(int id, CancellationToken ct);

    // סינון, מיון ו-paging. Skip/Take בתוך השאילתה
    Task<PagedResult<Slot>> GetPagedAsync(SlotQuery query, CancellationToken ct);

    // האם הזמן החדש חופף לסלוט קיים של אותה מחנכת. בעדכון מעבירים את מזהה הסלוט עצמו
    Task<bool> HasOverlapAsync(int teacherId, DateTime startUtc, DateTime endUtc, int? excludeSlotId, CancellationToken ct);

    // "תור אחד לילדה" (לא להורה-וילדה)
    Task<bool> StudentHasBookingAsync(int studentId, CancellationToken ct);

    Task AddAsync(Slot slot, CancellationToken ct);
    void Remove(Slot slot);

    // זורק SlotConflictException כשיש התנגשות (Version או אינדקס ייחודי)
    Task SaveChangesAsync(CancellationToken ct);
}
