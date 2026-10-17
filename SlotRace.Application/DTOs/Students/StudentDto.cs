namespace SlotRace.Application.DTOs.Students;

public class StudentDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int HomeroomTeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;

    // רק למחנכת. להורה השירות משאיר null (T18)
    public string? JoinCode { get; set; }
    public int? ParentsCount { get; set; }
}
