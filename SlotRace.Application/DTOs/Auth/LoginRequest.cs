using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Auth;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    // בלי MinLength: לא מגלים בהתחברות מה כללי הסיסמה
    [Required]
    public string Password { get; set; } = string.Empty;
}
