using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Domain.Enums;

namespace SlotRace.Application.Interfaces;

// המשתמשת המחוברת (userId, role) מגיעה מה-Controller, שקורא אותה מה-token.
// השירות עצמו לא מכיר HTTP
public interface ISlotService
{
    Task<ServiceResult<PagedResult<SlotDto>>> GetPagedAsync(SlotQuery query, int userId, Role role, CancellationToken ct);
    Task<ServiceResult<SlotDto>> GetByIdAsync(int id, int userId, CancellationToken ct);
    Task<ServiceResult<SlotDto>> CreateAsync(CreateSlotRequest request, int teacherId, CancellationToken ct);
    Task<ServiceResult<SlotDto>> UpdateAsync(int id, UpdateSlotRequest request, int teacherId, CancellationToken ct);

    // ServiceResult בלי T: אין ערך להחזיר, וה-Controller יחזיר 204
    Task<ServiceResult> DeleteAsync(int id, int teacherId, CancellationToken ct);
    Task<ServiceResult> CancelBookingAsync(int id, int parentId, CancellationToken ct);

    // BookAsync יתווסף ב-T17
}
