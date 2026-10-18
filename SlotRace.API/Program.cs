using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SlotRace.API.Options;
using SlotRace.Application.Common.Options;
using SlotRace.Application.Interfaces;
using SlotRace.Application.Mapping;
using SlotRace.Application.Services;
using SlotRace.Infrastructure.Persistence;
using SlotRace.Infrastructure.Repositories;
using SlotRace.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));


// הגדרות: כל חלק בקונפיגורציה נקשר למחלקה מוקלדת, ומוזרק כ-IOptions<T>.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RegistrationOptions>(builder.Configuration.GetSection(RegistrationOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();

// טרנזקציה: Scoped, כדי לקבל את אותו DbContext כמו ה-repositories
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// אבטחה: כל ממשק מ-Application מקבל את המימוש שלו מ-Infrastructure
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// שעון המערכת. Singleton כי אין לו מצב. בבדיקות מחליפים אותו בשעון מזויף
builder.Services.AddSingleton(TimeProvider.System);

// שירותים (לוגיקה עסקית)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISlotService, SlotService>();

// AutoMapper: רישום ה-Profile, ו-IMapper יוזרק לשירותים
builder.Services.AddAutoMapper(c =>
{
    c.AddProfile<MappingProfile>();
});

// ---------- Authentication: בדיקת ה-token בכל בקשה (הצד השני של JwtTokenGenerator) ----------

// את הבדיקה מגדירים בעליית השרת, ולכן קוראים את ההגדרות ישירות ולא דרך IOptions
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("חסר החלק Jwt בהגדרות.");

// HmacSha256 דורש מפתח של 32 תווים לפחות. עדיף שהשרת ייכשל מיד, ולא בהתחברות הראשונה
if (jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key חייב להיות באורך 32 תווים לפחות (ב-User Secrets).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,          // "האם אני הנפקתי אותו?"
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,      // "האם הוא מיועד לי?"
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)), // אותה "חותמת" כמו ב-T12
            ValidateLifetime = true,                  // "האם הוא עדיין בתוקף?"
            ClockSkew = TimeSpan.FromMinutes(1)       // סובלנות של דקה להבדלי שעון (ברירת המחדל: 5)
        };
    });

// Authorization: מאפשר [Authorize] ו-[Authorize(Roles = "Teacher")]
builder.Services.AddAuthorization();

// ---------- Swagger UI עם כפתור Authorize ----------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SlotRace API", Version = "v1" });

    // הגדרה: "יש באתר אימות מסוג Bearer token"
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "הדביקי כאן את ה-token מ-login (בלי המילה Bearer)."
    });

    // דרישה: "שלחי את ה-token בכל בקשה". בגרסה 10 מקבלים פונקציה עם document
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

// ---------- CORS: אישור לקליינט (כתובת אחרת) לפנות לשרת ----------

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy => policy
        .WithOrigins(corsOptions.AllowedOrigins)   // רק כתובות הקליינט מההגדרות
        .AllowAnyHeader()                          // כולל Authorization
        .AllowAnyMethod());                        // GET, POST, PUT, DELETE
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Swagger פעיל גם ב-Production בכוונה: פרויקט לימודי, כדי שאפשר יהיה לבדוק את השרת ב-Render.
// כל נקודת קצה עדיין דורשת token
app.UseSwagger();     // המסמך: /swagger/v1/swagger.json
app.UseSwaggerUI();   // המסך: /swagger

app.UseHttpsRedirection();

app.UseCors("Client");

app.UseAuthentication();   // קודם: מי את? (בונה את User מה-token)
app.UseAuthorization();    // אחר כך: מה מותר לך? (בודק את [Authorize])

app.MapControllers();

app.Run();
