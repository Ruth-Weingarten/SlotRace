using SlotRace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using SlotRace.Application.Common;
namespace SlotRace.Application.Interfaces
{
    public interface ISlotRepository
    {

        Task<Slot?> GetByIdAsync(int id, CancellationToken ct);
        Task<PagedResult<Slot>> GetPagedAsync(int? teacherId, bool onlyFree, int page, int pageSize, CancellationToken ct);
        Task<bool> ParentHasBookingForStudentAsync(int parentId, int studentId, CancellationToken ct);
        Task AddAsync(Slot slot, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
