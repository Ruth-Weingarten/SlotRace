using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Slots;

public class UpdateSlotRequest
{
    [Required]
    public DateTime? StartTimeUtc { get; set; }

    [Required]
    public DateTime? EndTimeUtc { get; set; }
}
