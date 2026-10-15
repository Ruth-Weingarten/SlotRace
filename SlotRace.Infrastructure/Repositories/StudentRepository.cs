using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace SlotRace.Infrastructure.Repositories
{
    public class StudentRepository: IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

         
        public async Task<Student?> GetWithParentsAsync(int id, CancellationToken ct)
        {
            return await _context.Students
                .AsNoTracking()
                .Include(s => s.Parents)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }
    }
}
