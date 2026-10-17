using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Slots;

public class CreateSlotRequest
{
    // DateTime? ולא DateTime, כדי ש-[Required] באמת יתפוס שדה חסר
    [Required]
    public DateTime? StartTimeUtc { get; set; }

    [Required]
    public DateTime? EndTimeUtc { get; set; }
}
