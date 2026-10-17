using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Students;

public class LinkStudentRequest
{
    [Required, StringLength(8, MinimumLength = 8)]
    public string JoinCode { get; set; } = string.Empty;
}
