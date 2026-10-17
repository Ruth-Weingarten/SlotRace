using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlotRace.Application.Common;
using SlotRace.Application.Common.Options;
using SlotRace.Application.DTOs.Auth;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Domain.Enums;

namespace SlotRace.Application.Services;

// Authentication: הרשמה, התחברות והנפקת token.
// הצורה של הבקשה (אימייל תקין, אורך סיסמה) כבר נבדקה ב-DTO, וכאן רק חוקים עסקיים.
public class AuthService : IAuthService
{
    // אותה הודעה לאימייל שגוי ולסיסמה שגויה, כדי לא לגלות אילו אימיילים רשומים
    private const string InvalidCredentialsMessage = "אימייל או סיסמה שגויים.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly RegistrationOptions _registration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IOptions<RegistrationOptions> registration,
        ILogger<AuthService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _registration = registration.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        // קוד מורה: ריק = הורה, נכון = מחנכת, שגוי = שגיאה.
        // נבדק לפני המסד, כי לא צריך בשבילו שאילתה
        Role role;
        if (string.IsNullOrWhiteSpace(request.TeacherCode))
        {
            role = Role.Parent;
        }
        else if (request.TeacherCode == _registration.TeacherCode)
        {
            role = Role.Teacher;
        }
        else
        {
            return ServiceResult<AuthResponse>.Fail(ErrorType.Validation, "קוד המורה שגוי.");
        }

        if (await _users.EmailExistsAsync(email, ct))
        {
            return ServiceResult<AuthResponse>.Fail(ErrorType.Conflict, "האימייל כבר רשום במערכת.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),   // הסיסמה עצמה לא נשמרת
            Role = role
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);   // רק כאן המסד נותן ל-user את ה-Id

        // structured logging: UserId ו-Role נשמרים כשדות. בלי סיסמה, בלי אימייל ובלי token
        _logger.LogInformation("User {UserId} registered as {Role}", user.Id, user.Role);

        // ההרשמה מחזירה token, כך שהמשתמשת מחוברת מיד
        return ServiceResult<AuthResponse>.Ok(CreateResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, ct);

        // שני מקרי הכישלון מחזירים בדיוק אותה תשובה
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt");   // בלי האימייל ובלי הסיסמה
            return ServiceResult<AuthResponse>.Fail(ErrorType.Unauthorized, InvalidCredentialsMessage);
        }

        return ServiceResult<AuthResponse>.Ok(CreateResponse(user));
    }

    // " Leah@Mail.com " → "leah@mail.com". הרשמה והתחברות חייבות לנרמל באותה צורה
    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    // הרשמה והתחברות מסתיימות באותה תשובה, ולכן הקוד במקום אחד
    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAtUtc) = _tokenGenerator.Generate(user);   // פירוק ה-tuple מ-T12

        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
