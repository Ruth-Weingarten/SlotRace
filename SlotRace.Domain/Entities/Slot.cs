namespace SlotRace.Domain.Entities;
public class Slot
{

public int Id { get; set; }

public int TeacherId { get; set; }
public User Teacher { get; set; } = null!;

public DateTime StartTimeUtc { get; set; }
public DateTime EndTimeUtc { get; set; }

public int? BookedByParentId { get; set; }
public User? BookedByParent { get; set; }

public int? StudentId { get; set; }
public Student? Student { get; set; }

public Guid Version { get; set; } = Guid.NewGuid();
}

