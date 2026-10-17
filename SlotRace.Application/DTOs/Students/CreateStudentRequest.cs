using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Students;

public class CreateStudentRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}
