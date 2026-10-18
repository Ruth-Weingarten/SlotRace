using Microsoft.EntityFrameworkCore;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Infrastructure.Persistence;

namespace SlotRace.Infrastructure.Repositories;

public class StudentRepository : IStudentRepository
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
            .Include(s => s.HomeroomTeacher)   // בשביל TeacherName ב-StudentDto
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    // בלי AsNoTracking: EF צריך לזכור את רשימת ההורים המקורית,
    // כדי לגלות שנוספה הורה ולכתוב שורה ב-ParentStudent
    public async Task<Student?> GetByJoinCodeAsync(string joinCode, CancellationToken ct)
    {
        return await _context.Students
            .Include(s => s.Parents)
            .Include(s => s.HomeroomTeacher)
            .FirstOrDefaultAsync(s => s.JoinCode == joinCode, ct);
    }

    public async Task<List<Student>> GetForTeacherAsync(int teacherId, CancellationToken ct)
    {
        return await _context.Students
            .AsNoTracking()
            .Where(s => s.HomeroomTeacherId == teacherId)
            .Include(s => s.Parents)            // בשביל ParentsCount
            .Include(s => s.HomeroomTeacher)    // בשביל TeacherName
            .OrderBy(s => s.FullName)
            .ToListAsync(ct);
    }

    // Include יורד קומה אחת (הורה → ילדות), ThenInclude ממשיך מהקומה שנטענה (ילדה → מחנכת).
    // הכול בשאילתה אחת עם JOIN, בלי לולאה (בלי בעיית N+1)
    public async Task<List<Student>> GetForParentAsync(int parentId, CancellationToken ct)
    {
        var parent = await _context.Users
            .AsNoTracking()
            .Include(u => u.Children)
                .ThenInclude(s => s.HomeroomTeacher)
            .FirstOrDefaultAsync(u => u.Id == parentId, ct);

        return parent?.Children.OrderBy(s => s.FullName).ToList() ?? new List<Student>();
    }

    public async Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken ct)
    {
        return await _context.Students.AnyAsync(s => s.JoinCode == joinCode, ct);
    }

    public async Task AddAsync(Student student, CancellationToken ct)
    {
        await _context.Students.AddAsync(student, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
