using Microsoft.EntityFrameworkCore;
using SlotRace.Application.Common;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace SlotRace.Infrastructure.Repositories
{
    public class SlotRepository : ISlotRepository
    {
        private readonly AppDbContext _context;

        public SlotRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Slot slot, CancellationToken ct)
        {
            await _context.Slots.AddAsync(slot, ct);
        }

        public async Task<Slot?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _context.Slots.FirstOrDefaultAsync(x => x.Id == id, ct);

        }

        public async Task<PagedResult<Slot>> GetPagedAsync(int? teacherId, bool onlyFree, int page, int pageSize, CancellationToken ct)
        {
            IQueryable<Slot> query = _context.Slots.AsNoTracking();
            if (teacherId.HasValue)
            {
                query = query.Where(x => x.TeacherId == teacherId.Value);
            }
            if (onlyFree)
            {
                query = query.Where(x => x.BookedByParentId == null);
            }
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(s => s.StartTimeUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.Teacher)
                .ToListAsync(ct);
            return new PagedResult<Slot>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> ParentHasBookingForStudentAsync(int parentId, int studentId, CancellationToken ct)
        {
            return await _context.Slots.AnyAsync<Slot>(x => x.BookedByParentId == parentId && x.StudentId == studentId, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
