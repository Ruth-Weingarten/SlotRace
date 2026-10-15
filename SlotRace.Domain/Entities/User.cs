using SlotRace.Domain.Enums;

namespace SlotRace.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }

    public List<Student> HomeroomStudents { get; set; } = new();
    public List<Student> Children { get; set; } = new();
    public List<Slot> Slots { get; set; } = new();
    public List<Slot> BookedSlots { get; set; } = new();

}