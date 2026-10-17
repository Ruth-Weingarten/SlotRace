using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SlotRace.Application.Common.Options;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;

namespace SlotRace.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    // ההגדרות (T9) והשעון מגיעים בהזרקת תלויות
    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public (string Token, DateTime ExpiresAtUtc) Generate(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(_options.ExpiryMinutes);

        // מה כתוב על ה"כרטיס". התוכן חתום אבל לא מוצפן, ולכן אין כאן סיסמה או מידע רגיש
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())   // "Parent" או "Teacher"
        };

        // ה"חותמת": המפתח הסודי מ-User Secrets (לפחות 32 תווים עבור HmacSha256)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        // ה"טופס": מתאר איך ה-token צריך להיראות. הוא עצמו לא יוצר כלום
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,       // מי הנפיקה
            Audience = _options.Audience,   // למי הוא מיועד
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        // ה"דפוס": יוצר את המחרוזת (כותרת.תוכן.חתימה)
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAtUtc);
    }
}
