using SlotRace.Domain.Entities;

namespace SlotRace.Application.Interfaces;

public interface IStudentRepository
{
    // קריאה בלבד, עם ההורים והמחנכת (לבדיקת הרשאה ול-StudentDto)
    Task<Student?> GetWithParentsAsync(int id, CancellationToken ct);

    // לקישור הורה: עם מעקב, כדי ש-Parents.Add יישמר
    Task<Student?> GetByJoinCodeAsync(string joinCode, CancellationToken ct);

    Task<List<Student>> GetForTeacherAsync(int teacherId, CancellationToken ct);

    // Include + ThenInclude: הורה → הילדות → המחנכת של כל ילדה
    Task<List<Student>> GetForParentAsync(int parentId, CancellationToken ct);

    Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken ct);
    Task AddAsync(Student student, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
