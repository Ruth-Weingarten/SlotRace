using System.Security.Cryptography;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Students;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Domain.Enums;

namespace SlotRace.Application.Services;

public class StudentService : IStudentService
{
    // בלי 0/O ובלי 1/I, כדי שלא יתבלבלו כשמעתיקים מפתק
    private const string JoinCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int JoinCodeLength = 8;

    private readonly IStudentRepository _students;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        IStudentRepository students,
        IUserRepository users,
        IMapper mapper,
        ILogger<StudentService> logger)
    {
        _students = students;
        _users = users;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<StudentDto>> CreateAsync(CreateStudentRequest request, int teacherId, CancellationToken ct)
    {
        var student = _mapper.Map<Student>(request);      // FullName (T11)
        student.FullName = student.FullName.Trim();
        student.HomeroomTeacherId = teacherId;            // מה-token: המחנכת היוצרת היא המחנכת שלה
        student.JoinCode = await GenerateUniqueJoinCodeAsync(ct);

        await _students.AddAsync(student, ct);
        await _students.SaveChangesAsync(ct);

        _logger.LogInformation("Student {StudentId} created by teacher {TeacherId}", student.Id, teacherId);

        // הצבנו רק HomeroomTeacherId (מספר), ולכן טוענים מחדש בשביל שם המחנכת. כמו ב-SlotService
        var created = await _students.GetWithParentsAsync(student.Id, ct);
        return ServiceResult<StudentDto>.Ok(ToDto(created!, teacherId));
    }

    public async Task<ServiceResult<List<StudentDto>>> GetMineAsync(int userId, Role role, CancellationToken ct)
    {
        var students = role == Role.Teacher
            ? await _students.GetForTeacherAsync(userId, ct)    // התלמידות שלה, עם ההורים
            : await _students.GetForParentAsync(userId, ct);    // הילדות שלה, עם המחנכת (ThenInclude)

        return ServiceResult<List<StudentDto>>.Ok(students.Select(s => ToDto(s, userId)).ToList());
    }

    public async Task<ServiceResult<StudentDto>> GetByIdAsync(int id, int userId, CancellationToken ct)
    {
        var student = await _students.GetWithParentsAsync(id, ct);
        if (student is null)
            return ServiceResult<StudentDto>.Fail(ErrorType.NotFound, "התלמידה לא נמצאה.");

        // רק המחנכת שלה או הורה מקושרת
        var allowed = student.HomeroomTeacherId == userId || student.Parents.Any(p => p.Id == userId);
        if (!allowed)
            return ServiceResult<StudentDto>.Fail(ErrorType.Forbidden, "אין לך הרשאה לצפות בתלמידה הזאת.");

        return ServiceResult<StudentDto>.Ok(ToDto(student, userId));
    }

    public async Task<ServiceResult<StudentDto>> LinkAsync(LinkStudentRequest request, int parentId, CancellationToken ct)
    {
        // הקודים נשמרים באותיות גדולות. כך גם "k7m4xp2a" יעבוד
        var code = request.JoinCode.Trim().ToUpperInvariant();

        var student = await _students.GetByJoinCodeAsync(code, ct);   // עם מעקב, כדי ש-Parents.Add יישמר
        if (student is null)
            return ServiceResult<StudentDto>.Fail(ErrorType.NotFound, "קוד ההצטרפות לא נמצא.");

        if (student.Parents.Any(p => p.Id == parentId))
            return ServiceResult<StudentDto>.Fail(ErrorType.Conflict, "את כבר מקושרת לתלמידה הזאת.");

        // Parents היא List<User>, ולכן צריך את האובייקט של ההורה ולא רק את המספר.
        // אותו DbContext (Scoped) כמו של StudentRepository, ולכן SaveChanges אחד שומר הכול
        var parent = await _users.GetByIdAsync(parentId, ct);
        if (parent is null)
            return ServiceResult<StudentDto>.Fail(ErrorType.NotFound, "המשתמשת לא נמצאה.");

        student.Parents.Add(parent);              // EF יכתוב שורה ב-ParentStudent
        await _students.SaveChangesAsync(ct);

        _logger.LogInformation("Parent {ParentId} linked to student {StudentId}", parentId, student.Id);

        return ServiceResult<StudentDto>.Ok(ToDto(student, parentId));   // בלי JoinCode: היא לא המחנכת
    }

    // ---------- פונקציות עזר ----------

    // RandomNumberGenerator (קריפטוגרפי) ולא Random: הקוד הוא "מפתח" לילדה, ולכן צריך שיהיה בלתי צפוי
    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken ct)
    {
        string code;
        do
        {
            code = RandomNumberGenerator.GetString(JoinCodeChars, JoinCodeLength);
        }
        while (await _students.JoinCodeExistsAsync(code, ct));   // תפוס? מגרילים שוב

        return code;
    }

    // secure by default: JoinCode ו-ParentsCount ריקים (Ignore ב-T11), ומתמלאים רק למחנכת של התלמידה
    private StudentDto ToDto(Student student, int userId)
    {
        var dto = _mapper.Map<StudentDto>(student);

        if (student.HomeroomTeacherId == userId)
        {
            dto.JoinCode = student.JoinCode;
            dto.ParentsCount = student.Parents.Count;
        }

        return dto;
    }
}
