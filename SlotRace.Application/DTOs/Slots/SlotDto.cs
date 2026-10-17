namespace SlotRace.Application.DTOs.Slots;

public class SlotDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public bool IsBooked { get; set; }

    // שני השדות האלה תלויים במשתמשת המחוברת, ולכן השירות קובע אותם (לא AutoMapper)
    public bool BookedByMe { get; set; }
    public string? StudentName { get; set; }   // null למי שאסור לה לראות
}
