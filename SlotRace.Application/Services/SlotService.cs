using AutoMapper;
using Microsoft.Extensions.Logging;
using SlotRace.Application.Common;
using SlotRace.Application.Common.Exceptions;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Domain.Enums;

namespace SlotRace.Application.Services;

// סדר הבדיקות בכל פעולה: קיים? (404) → שלך? (403) → מצב תקין? (409/400) → מבצעים
public class SlotService : ISlotService
{
    private const int MaxPageSize = 50;

    private readonly ISlotRepository _slots;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;   // במקום DateTime.UtcNow, כדי שבבדיקות אפשר "לקבוע" את השעה
    private readonly ILogger<SlotService> _logger;

    public SlotService(ISlotRepository slots, IMapper mapper, TimeProvider timeProvider, ILogger<SlotService> logger)
    {
        _slots = slots;
        _mapper = mapper;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<SlotDto>>> GetPagedAsync(SlotQuery query, int userId, Role role, CancellationToken ct)
    {
        // הגנה כפולה: גם ה-DTO מגביל ל-50 ([Range]), וגם השירות
        query.PageSize = Math.Min(query.PageSize, MaxPageSize);

        // מחנכת שלא ביקשה מחנכת מסוימת רואה את הסלוטים שלה
        if (role == Role.Teacher && query.TeacherId is null)
            query.TeacherId = userId;

        var page = await _slots.GetPagedAsync(query, ct);   // AsNoTracking: רק קריאה

        var result = new PagedResult<SlotDto>
        {
            Items = page.Items.Select(s => ToDto(s, userId)).ToList(),   // כל סלוט עובר דרך הפרטיות
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };

        return ServiceResult<PagedResult<SlotDto>>.Ok(result);
    }

    public async Task<ServiceResult<SlotDto>> GetByIdAsync(int id, int userId, CancellationToken ct)
    {
        var slot = await _slots.GetDetailsAsync(id, ct);
        if (slot is null)
            return ServiceResult<SlotDto>.Fail(ErrorType.NotFound, "הסלוט לא נמצא.");

        return ServiceResult<SlotDto>.Ok(ToDto(slot, userId));
    }

    public async Task<ServiceResult<SlotDto>> CreateAsync(CreateSlotRequest request, int teacherId, CancellationToken ct)
    {
        // [Required] ב-DTO כבר וידא שהשדות נשלחו, ולכן ! ו-.Value בטוחים
        var start = request.StartTimeUtc!.Value;
        var end = request.EndTimeUtc!.Value;

        var timeError = ValidateTimes(start, end);
        if (timeError is not null)
            return ServiceResult<SlotDto>.Fail(ErrorType.Validation, timeError);

        if (await _slots.HasOverlapAsync(teacherId, start, end, excludeSlotId: null, ct))
            return ServiceResult<SlotDto>.Fail(ErrorType.Conflict, "הסלוט חופף לסלוט קיים שלך.");

        var slot = _mapper.Map<Slot>(request);   // מעתיק את שני הזמנים (T11)
        slot.TeacherId = teacherId;              // מה-token, אף פעם לא מהבקשה

        await _slots.AddAsync(slot, ct);
        await _slots.SaveChangesAsync(ct);       // כאן הסלוט מקבל Id

        _logger.LogInformation("Slot {SlotId} created by teacher {TeacherId}", slot.Id, teacherId);

        // הצבנו רק TeacherId (מספר), ולכן slot.Teacher הוא null.
        // טוענים מחדש עם Include, כדי שיהיה שם המחנכת ב-SlotDto
        var created = await _slots.GetDetailsAsync(slot.Id, ct);
        return ServiceResult<SlotDto>.Ok(ToDto(created!, teacherId));
    }

    public async Task<ServiceResult<SlotDto>> UpdateAsync(int id, UpdateSlotRequest request, int teacherId, CancellationToken ct)
    {
        var slot = await _slots.GetForUpdateAsync(id, ct);   // עם מעקב, כי משנים ושומרים
        if (slot is null)
            return ServiceResult<SlotDto>.Fail(ErrorType.NotFound, "הסלוט לא נמצא.");

        if (slot.TeacherId != teacherId)
            return ServiceResult<SlotDto>.Fail(ErrorType.Forbidden, "אפשר לשנות רק סלוט שלך.");

        if (slot.BookedByParentId is not null)
            return ServiceResult<SlotDto>.Fail(ErrorType.Conflict, "אי אפשר לשנות סלוט שכבר הוזמן.");

        var start = request.StartTimeUtc!.Value;
        var end = request.EndTimeUtc!.Value;

        var timeError = ValidateTimes(start, end);
        if (timeError is not null)
            return ServiceResult<SlotDto>.Fail(ErrorType.Validation, timeError);

        if (await _slots.HasOverlapAsync(teacherId, start, end, excludeSlotId: id, ct))   // לא להשוות לעצמו
            return ServiceResult<SlotDto>.Fail(ErrorType.Conflict, "הסלוט חופף לסלוט קיים שלך.");

        // בלי AutoMapper (החלטה מ-T11): שני שדות, מפורש
        slot.StartTimeUtc = start;
        slot.EndTimeUtc = end;

        try
        {
            await _slots.SaveChangesAsync(ct);
        }
        catch (SlotConflictException)
        {
            // הורה הזמינה את הסלוט בדיוק בזמן שהמחנכת שינתה אותו (ה-Version השתנה)
            return ServiceResult<SlotDto>.Fail(ErrorType.Conflict, "הסלוט השתנה בינתיים. רענני ונסי שוב.");
        }

        return ServiceResult<SlotDto>.Ok(ToDto(slot, teacherId));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int teacherId, CancellationToken ct)
    {
        var slot = await _slots.GetForUpdateAsync(id, ct);
        if (slot is null)
            return ServiceResult.Fail(ErrorType.NotFound, "הסלוט לא נמצא.");

        if (slot.TeacherId != teacherId)
            return ServiceResult.Fail(ErrorType.Forbidden, "אפשר למחוק רק סלוט שלך.");

        if (slot.BookedByParentId is not null)
            return ServiceResult.Fail(ErrorType.Conflict, "אי אפשר למחוק סלוט שכבר הוזמן.");

        _slots.Remove(slot);

        try
        {
            // Version נבדק גם ב-DELETE: אם הסלוט הוזמן בינתיים, 0 שורות נמחקות
            await _slots.SaveChangesAsync(ct);
        }
        catch (SlotConflictException)
        {
            return ServiceResult.Fail(ErrorType.Conflict, "הסלוט השתנה בינתיים. רענני ונסי שוב.");
        }

        _logger.LogInformation("Slot {SlotId} deleted by teacher {TeacherId}", id, teacherId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelBookingAsync(int id, int parentId, CancellationToken ct)
    {
        var slot = await _slots.GetForUpdateAsync(id, ct);
        if (slot is null)
            return ServiceResult.Fail(ErrorType.NotFound, "הסלוט לא נמצא.");

        if (slot.BookedByParentId != parentId)
            return ServiceResult.Fail(ErrorType.Forbidden, "אפשר לבטל רק תור שהזמנת.");

        if (slot.StartTimeUtc <= _timeProvider.GetUtcNow().UtcDateTime)
            return ServiceResult.Fail(ErrorType.Validation, "אי אפשר לבטל תור שכבר עבר.");

        slot.BookedByParentId = null;
        slot.StudentId = null;

        try
        {
            await _slots.SaveChangesAsync(ct);
        }
        catch (SlotConflictException)
        {
            return ServiceResult.Fail(ErrorType.Conflict, "הסלוט השתנה בינתיים. רענני ונסי שוב.");
        }

        _logger.LogInformation("Booking on slot {SlotId} cancelled by parent {ParentId}", id, parentId);
        return ServiceResult.Ok();
    }

    // ---------- פונקציות עזר ----------

    // ולידציה עסקית של זמנים (תלויה בשעה הנוכחית, ולכן לא ב-DTO). null = תקין
    private string? ValidateTimes(DateTime startUtc, DateTime endUtc)
    {
        // Npgsql מסרב לשמור DateTime שאינו UTC. עדיף 400 ברור מאשר 500 מהמסד
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
            return "הזמנים חייבים להיות ב-UTC (עם Z בסוף, למשל 2026-10-20T08:00:00Z).";

        if (endUtc <= startUtc)
            return "שעת הסיום חייבת להיות אחרי שעת ההתחלה.";

        if (startUtc <= _timeProvider.GetUtcNow().UtcDateTime)
            return "אי אפשר ליצור סלוט בעבר.";

        return null;
    }

    // secure by default: כל SlotDto שיוצא מהשירות נבנה כאן בלבד.
    // שם הילדה רק למחנכת של הסלוט ולהורה שהזמינה
    private SlotDto ToDto(Slot slot, int userId)
    {
        var dto = _mapper.Map<SlotDto>(slot);

        dto.BookedByMe = slot.BookedByParentId == userId;

        var canSeeStudent = slot.TeacherId == userId || slot.BookedByParentId == userId;
        dto.StudentName = canSeeStudent ? slot.Student?.FullName : null;

        return dto;
    }
}
