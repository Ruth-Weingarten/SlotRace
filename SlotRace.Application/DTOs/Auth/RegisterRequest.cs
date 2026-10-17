using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // אופציונלי: ריק = הורה. הבדיקה אם הקוד נכון נעשית ב-AuthService (T13)
    public string? TeacherCode { get; set; }
}
