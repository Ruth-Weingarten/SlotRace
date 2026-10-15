using SlotRace.Domain.Enums;

namespace SlotRace.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;

    public int HomeroomTeacherId { get; set; }
    public User HomeroomTeacher { get; set; } = null!;

    public List<User> Parents { get; set; } = new();

}