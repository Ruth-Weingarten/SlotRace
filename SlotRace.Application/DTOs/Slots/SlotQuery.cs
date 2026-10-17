using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Slots;

public class SlotQuery
{
    public int? TeacherId { get; set; }
    public bool OnlyFree { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    public bool SortDesc { get; set; }
}
