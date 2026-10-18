# SlotRace – אפיון ותוכנית עבודה (גרסה 2)

מערכת לקביעת תורים לאסיפת הורים. מחנכות פותחות סלוטים (חלונות זמן), והורים קובעות לעצמן תור אוטומטית, בלי אישור מזכירה.

- **המשאב המוגבל:** הסלוט. אפשר להזמין כל סלוט פעם אחת בלבד.
- **מבחן המשפט (דרישת המורה):** "שתי אמהות הזמינו באותו רגע את אותו סלוט, שתיהן קיבלו אישור, ואסור היה ששתיהן יקבלו."
- **המשפט השני (חדש):** "אבא ואמא של אותה ילדה הזמינו באותו רגע שני סלוטים שונים, ושניהם קיבלו אישור, ואסור היה, כי מותר תור אחד לילדה." (ראו T22)

גרסה 2 נבנתה מתוך מסמך הדרישות של המורה (`final-project-dotnet.docx`) ומהאפיון הקודם. בגרסה הזאת כל דרישה של המורה ממופה למשימה (ראו "טבלת כיסוי" בסוף).

סימון: `[x]` = בוצע ונבדק · `[~]` = בוצע חלקית · `[ ]` = עוד לא בוצע.
כל משימה היא commit אחד, בפורמט `T<מספר>: <מה נעשה>`, ואחריו push.

---

## איך עובדים

1. משימה אחת בכל פעם, לפי הסדר.
2. לפני הקוד: הסבר קצר במילים פשוטות, אילו קבצים ייווצרו ובאיזו שכבה, ולמה.
3. Claude כותבת את הקוד. רות קוראת, שואלת עד שהיא מבינה, ומאשרת.
4. `dotnet build` ירוק. **Claude עושה את ה-commit** (בפורמט `T<מספר>: ...`). **ה-push נעשה על ידי רות** (`git push` בטרמינל), כי לסביבה של Claude אין גישה לרשת של GitHub.
   **הערה טכנית:** ב-shell שבו Claude עובדת על המחשב אין כרגע `dotnet`. את ה-build מריצים ב-Visual Studio, או ש-Claude מתקינה .NET SDK 10 ב-shell בתחילת השיחה הבאה.
5. בסוף כל משימה: לעדכן את האפיון (`[x]` ו"מצב נוכחי"), להציע הודעת commit, ולשאול שאלת הבנה אחת.

---

## מצב נוכחי (נבדק מול הקוד ב-15.10.2026)

| מה | מצב |
|---|---|
| Solution `SlotRace.slnx` (.NET 10) עם ארבעה פרויקטים והפניות נכונות | קיים ✓ |
| ישויות: `User`, `Student`, `Slot`, `Role` (`Parent` ראשון) | קיים ✓ |
| `AppDbContext`: Fluent API, `ParentStudent` (many-to-many), שני קשרים `User`↔`Slot`, `Restrict`, אינדקסים ייחודיים על `Email` ו-`JoinCode`, `Version` כ-concurrency token, override של `SaveChangesAsync` | קיים ✓ |
| PostgreSQL ב-Docker (`pg-slotrace`), connection string ב-User Secrets, migration `Init` | קיים ✓ |
| ממשקי repositories ב-`Application` ומימושים ב-`Infrastructure`, רישום `AddScoped` | קיים ✓ |
| `appsettings.json` בלי סודות | נבדק ✓ |
| גיט מקומי: `.gitignore`, `.gitattributes`, commit ראשון `bdeafc1`, האפיון ב-`docs/spec.md` | קיים ✓ |
| repository ב-GitHub (`Ruth-Weingarten/SlotRace`, Public) ו-push | קיים ✓ (16.10.2026) |
| כל השאר (Services, DTOs, JWT, Controllers, Middleware, NLog, Seed, בדיקות, README, קליינט, Render) | **עוד לא** |

**ממצאים קטנים בקוד הקיים (ארבעת הראשונים טופלו ב-T7, 16.10.2026):**
- `SlotRepository.cs`: `using System.Reflection.Metadata.Ecma335;` לא בשימוש.
- `Slot.cs`: `using SlotRace.Domain.Entities;` מיותר, כי הקובץ כבר נמצא ב-namespace הזה.
- `Student.cs`: `using SlotRace.Domain.Enums;` לא בשימוש.
- `SlotRace.Domain.csproj`: שארית `<Folder Include="Entities\" />`.
- `ISlotRepository.ParentHasBookingForStudentAsync` בודק "להורה הזאת יש תור לילדה", אבל הכלל שנקבע הוא "תור אחד **לילדה**". יוחלף ב-T15.

---

## מיפוי השכבות: דרישת המורה ↔ Clean Architecture

המורה כתבה שמותר, ואפילו מועדף, לבנות לפי החלוקה החדשה. זו החלוקה שבחרנו. במבחן חשוב לדעת להסביר איך היא מתאימה לחלוקה של המורה:

| אצל המורה | אצלנו | מה יושב שם |
|---|---|---|
| `Core` (Entities, Enums) | `SlotRace.Domain` | ישויות ו-enums בלבד. אין חבילות, אין הפניות. |
| `Core` (Interfaces, DTOs) | `SlotRace.Application` | ממשקי repositories ושירותים, DTOs, `ServiceResult`, `PagedResult`, AutoMapper Profile, מחלקות Options |
| `Service` | `SlotRace.Application/Services` | הלוגיקה העסקית: `AuthService`, `SlotService`, `StudentService` |
| `Data` | `SlotRace.Infrastructure` | `AppDbContext`, Repositories, Migrations, Seeder, וגם מימושים "טכניים": BCrypt ו-JWT |
| `API` | `SlotRace.API` | Controllers, Middleware, `Program.cs`, `nlog.config` |

**כיוון התלויות:**
```
API ──► Application ──► Domain
 │            ▲
 └──► Infrastructure   (API מכירה את Infrastructure רק לרישום ב-Program.cs)
```

**למה הממשקים ב-Application ולא ב-Infrastructure (שאלה צפויה במבחן):** השירות (`SlotService`) צריך repository. אם הממשק היה יושב ב-Infrastructure, `Application` הייתה צריכה להפנות ל-Infrastructure, וכך הייתה נקשרת ל-EF. כשהממשק יושב ב-Application, השירות מכיר רק "חוזה". Infrastructure היא זו שמתחייבת לממש אותו, ולכן התלות הפוכה. זה עקרון היפוך התלויות (Dependency Inversion).

**שתי טעויות שהמורה אמרה שיורידו ניקוד, ואיך נמנעים מהן:**
1. Core (אצלנו Domain ו-Application) עם חבילת EF: `Application` לא תכיר את `DbUpdateConcurrencyException`. ה-repository יתרגם אותה לחריגה שלנו (T15).
2. Controller שמזריק `DbContext`: אצלנו Controller מזריק רק `I...Service`.

---

## החלטות שהתקבלו (לא לשנות בלי לשאול)

- **ארבעה פרויקטים** (ראו מיפוי), ועוד פרויקט חמישי לבדיקות: `SlotRace.Tests`. הוא לא חלק מהאפליקציה.
- **ממשקים** ב-`Application`, מימושים ב-`Infrastructure`. `Domain` בלי חבילות.
- **Application לא מכירה HTTP, EF, BCrypt או JWT.** סיסמאות ו-tokens עוברים דרך ממשקים (`IPasswordHasher`, `IJwtTokenGenerator`) שמומשו ב-Infrastructure.
- **שירות מחזיר `ServiceResult<T>`** עם `ErrorType`, וה-Controller מתרגם לסטטוס HTTP.
- **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`), גם מקומית ב-Docker וגם ב-Render. כל הזמנים ב-UTC.
- **סודות** (connection string, `Jwt:Key`, `Registration:TeacherCode`) נמצאים רק ב-User Secrets. ב-Render הם משתני סביבה.
- **הגדרות** נקראות דרך `IOptions<T>`: `JwtOptions`, `RegistrationOptions`, `CorsOptions`.
- **ישות `User` אחת עם `Role`** (`Parent`, `Teacher`). `Parent` ראשון ב-enum, כי זו ההרשאה הנמוכה.
- **הרשמה עצמית:** מחנכת מזינה `TeacherCode` סודי. בלעדיו המשתמשת היא `Parent`, וקוד שגוי מחזיר שגיאת ולידציה.
- **תלמידות:** המחנכת יוצרת תלמידה, והיא נקבעת אוטומטית כמחנכת שלה. לכל תלמידה נוצר `JoinCode` ייחודי. הורה מזינה את הקוד ומתקשרת לילדה. הקשר הוא many-to-many.
- **הזמנה אוטומטית:** הורה מזמינה רק אצל המחנכת של ילדה שלה, ורק תור אחד לכל ילדה.
- **התחרות (optimistic concurrency):** `Version` (Guid) מוגדר כ-`IsConcurrencyToken()`. הוא מוחלף ב-override של `SaveChangesAsync` לכל `Slot` במצב `Modified`. `DbUpdateConcurrencyException` מתורגמת ל-409. זו "הדרך השנייה" בנספח של המורה.
- **אסינכרוני** לכל אורך השרשרת עם `CancellationToken`. בלי `.Result` ובלי `.Wait()`.
- **`AsNoTracking`** בשאילתות קריאה בלבד. כשטוענים סלוט כדי לשנות אותו, **בלי** `AsNoTracking`.
- **Lifetimes:** הכול `Scoped`, חוץ מ-`TimeProvider.System`, שהוא Singleton כי אין לו מצב.
- **DTOs:** ישויות לא יוצאות מה-API ולא נכנסות אליו.
- **שני repositories ב-GitHub:** אחד לשרת ואחד לקליינט (דרישת המורה).

---

## נקודות הקצה (REST)

כל נקודות הקצה מחייבות token, חוץ מהרשמה והתחברות.

| Method | URL | מי | הצלחה | שגיאות אפשריות |
|---|---|---|---|---|
| POST | `/api/auth/register` | כולם | 200 + token | 400, 409 (אימייל קיים) |
| POST | `/api/auth/login` | כולם | 200 + token | 400, 401 |
| GET | `/api/slots?teacherId&onlyFree&fromUtc&toUtc&page&pageSize&sortDesc` | מחוברת | 200 + `PagedResult<SlotDto>` | 400 |
| GET | `/api/slots/{id}` | מחוברת | 200 | 404 |
| POST | `/api/slots` | Teacher | **201 + Location** | 400, 409 (חפיפה) |
| PUT | `/api/slots/{id}` | Teacher (שלה) | 200 | 400, 403, 404, 409 (תפוס, חפיפה, התנגשות) |
| DELETE | `/api/slots/{id}` | Teacher (שלה) | **204** | 403, 404, 409 (תפוס) |
| POST | `/api/slots/{id}/booking` | Parent | 200 + `SlotDto` | 400, 403, 404, **409** |
| DELETE | `/api/slots/{id}/booking` | Parent (שהזמינה) | **204** | 400, 403, 404, 409 |
| GET | `/api/students` | מחוברת | 200 (מחנכת: התלמידות שלה; הורה: הילדות שלה) | – |
| GET | `/api/students/{id}` | המחנכת או הורה מקושרת | 200 | 403, 404 |
| POST | `/api/students` | Teacher | **201 + Location** | 400 |
| POST | `/api/student-links` | Parent | 200 + `StudentDto` | 400, 404 (קוד לא קיים), 409 (כבר מקושרת) |

- **401:** אין token, או שה-token לא תקף (נבדק אוטומטית על ידי `[Authorize]`). **403:** role לא מתאים (`[Authorize(Roles = "Teacher")]`), או בדיקת בעלות בשירות.
- **`booking` ביחיד:** לסלוט יש לכל היותר הזמנה אחת, ולכן זה תת-משאב יחיד, כמו `/users/{id}/profile`. הפעולה היא HTTP method (POST מזמין, DELETE מבטל) ולא פועל ב-URL. ראו החלטה פתוחה 8.
- **פרטיות:** ב-`SlotDto` שם הילדה מוצג רק למחנכת של הסלוט ולהורה שהזמינה. כל השאר רואות רק `IsBooked`.

---

## משימות

### שלב א – יסודות

### T0 – גיט ו-GitHub `[x]`
**בוצע:** `.gitignore`, `.gitattributes` (`* text=auto`, כדי שסופי שורות לא ייראו כשינוי), `git init`, ו-commit ראשון של T1 עד T6.
**בוצע ב-16.10.2026 (רות):**
1. ליצור ב-GitHub repository בשם `SlotRace`, **Public**, בלי README, בלי `.gitignore` ובלי license.
2. `git remote add origin https://github.com/Ruth-Weingarten/SlotRace.git`
3. `git push -u origin main`

**קריטריון סיום:** הקוד נמצא ב-GitHub, ואין בו `bin`, `obj`, `.vs` או סודות.

### T1–T6 `[x]`
Solution, ישויות, `AppDbContext`, מסד נתונים ו-migration `Init`, ממשקים, repositories (פירוט בגרסה 1).

### T7 – ניקוי הקוד הקיים `[x]`
מחיקת ה-`using`-ים שלא בשימוש ושארית ה-`<Folder>` (ראו "ממצאים"). אין שינוי בהתנהגות.
**למה:** כל שורה בפרויקט צריכה להיות שורה שאפשר להסביר.

### T8 – `ServiceResult` וחריגת ההתנגשות `[x]`
**Application/Common:**
- `enum ErrorType`: `None`, `Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`.
- `ServiceResult<T>`: `IsSuccess`, `Value`, `Error`, `Message`, ו-`Ok(value)` / `Fail(error, message)` סטטיים. בנוסף `ServiceResult` בלי `T`, לפעולות שלא מחזירות ערך, למשל מחיקה.

**Application/Common/Exceptions:**
- `SlotConflictException`: "החריגה שלנו". ה-repository זורק אותה במקום החריגה של EF, וכך `Application` לא צריכה להכיר את EF.

**למה:** כשל צפוי (לא נמצא, תפוס, אין הרשאה) הוא לא "חריגה". הוא תוצאה רגילה של העסק, ולכן מוחזר כערך. חריגות נשארות למקרים לא צפויים, ואותם מטפל ה-`ExceptionMiddleware`.

### T9 – הגדרות (Configuration ו-IOptions) `[x]`
- **Application/Common/Options:** `JwtOptions` (`Issuer`, `Audience`, `Key`, `ExpiryMinutes`), `RegistrationOptions` (`TeacherCode`). **API:** `CorsOptions` (`AllowedOrigins`).
- **`appsettings.json`:** הגדרות לא סודיות בלבד: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, `Cors:AllowedOrigins`.
- **`appsettings.Development.json`:** רמות לוג מפורטות יותר, ו-origin מקומי של הקליינט.
- **User Secrets:** `Jwt:Key` (לפחות 32 תווים), `Registration:TeacherCode`.
- **`Program.cs`:** `builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"))` וכן הלאה.

**למה IOptions:** השירות מקבל אובייקט מוקלד במקום מחרוזות כמו `config["Jwt:Key"]`. כך שגיאת כתיב בשם הגדרה מתגלה במקום אחד, וקל לדמות את ההגדרות בבדיקות.
**קריטריון סיום:** אין סוד באף קובץ ב-repo (בודקים עם `git grep`).

### T10 – DTOs `[x]`
**Application/DTOs**, מחולק לתיקיות:
- **Auth:** `RegisterRequest` (`FullName` [Required, MaxLength 100], `Email` [Required, EmailAddress], `Password` [Required, MinLength 8], `TeacherCode?`), `LoginRequest`, `AuthResponse` (`Token`, `ExpiresAtUtc`, `FullName`, `Role`).
- **Slots:** `CreateSlotRequest` / `UpdateSlotRequest` (`StartTimeUtc`, `EndTimeUtc` [Required]), `SlotQuery` (`TeacherId?`, `OnlyFree`, `FromUtc?`, `ToUtc?`, `Page` [Range 1..], `PageSize` [Range 1..50], `SortDesc`), `BookSlotRequest` (`StudentId` [Range 1..]), `SlotDto` (`Id`, `TeacherId`, `TeacherName`, `StartTimeUtc`, `EndTimeUtc`, `IsBooked`, `BookedByMe`, `StudentName?`).
- **Students:** `CreateStudentRequest` (`FullName`), `LinkStudentRequest` (`JoinCode`), `StudentDto` (`Id`, `FullName`, `HomeroomTeacherId`, `TeacherName`, `JoinCode?` (למחנכת בלבד), `ParentsCount?`).

**חלוקת הולידציה (דרישה 2):** Data Annotations בודקות **צורה**: שדה חובה, אורך, טווח. ולידציה **עסקית**, שתלויה במצב הנתונים (הסלוט בעבר, תפוס, חופף), יושבת בשירות ולא ב-Controller.
`PasswordHash` לא מופיע באף DTO.

**החלטות שהתקבלו ב-T10:**
- כל ה-DTOs הם `class` עם `get; set;` (לא `record`), כדי שיתאימו ל-model binding ול-AutoMapper.
- `StartTimeUtc` / `EndTimeUtc` בבקשות הסלוט הם `DateTime?`, כי `[Required]` על `DateTime` רגיל לא תופס שדה חסר.
- `AuthResponse.Role` הוא ה-enum `Role` מ-Domain (ולא `string`).
- תוספות: `CreateStudentRequest.FullName` [Required, MaxLength 100], `LinkStudentRequest.JoinCode` [Required, StringLength 8, MinimumLength 8], `PageSize` ברירת מחדל 10, `Page` ברירת מחדל 1.

### T11 – AutoMapper `[x]`
חבילה ב-`Application`, ו-`MappingProfile`:
- `Slot` → `SlotDto` (`TeacherName` מתוך `Teacher.FullName`, `IsBooked` = `BookedByParentId != null`).
- `Student` → `StudentDto`.
- `CreateSlotRequest` → `Slot`, `CreateStudentRequest` → `Student`.

`BookedByMe` ו-`StudentName` לא נקבעים במיפוי, כי הם תלויים במשתמשת המחוברת. השירות קובע אותם אחרי המיפוי.
רישום ב-`Program.cs`: `AddAutoMapper(c => c.AddProfile<MappingProfile>())`, כמו בכיתה.

**החלטות שהתקבלו ב-T11:**
- `AutoMapper` **16.2.0** בלי מפתח רישיון, כמו בפרויקט הכיתה (Clinic). החבילה מותקנת רק ב-Application.
- `JoinCode` ו-`ParentsCount` ב-`StudentDto` מסומנים `Ignore` (secure by default). השירות ממלא אותם רק למחנכת.
- בלי `ReverseMap`: ממפים רק בכיוונים שצריך.
- `UpdateSlotRequest` **לא ממופה**. ב-T16 השירות מעתיק את שני הזמנים ביד.

### שלב ב – משתמשות והרשאות

### T12 – הצפנת סיסמאות ויצירת token (מאחורי ממשקים) `[x]`
- **Application/Interfaces:** `IPasswordHasher` (`Hash`, `Verify`), `IJwtTokenGenerator` (`Generate(User) → (token, expiresAtUtc)`).
- **Infrastructure/Security:** `BCryptPasswordHasher` (חבילה `BCrypt.Net-Next`), `JwtTokenGenerator` (חבילה `System.IdentityModel.Tokens.Jwt`, מקבל `IOptions<JwtOptions>`). Claims: `NameIdentifier` (מזהה), `Email`, `Role`, `Name`.

**למה מאחורי ממשק:** כך `Application` נשארת בלי חבילות טכניות, ובבדיקות אפשר לדמות את שתי הפעולות עם Moq.

**החלטות שהתקבלו ב-T12:**
- במקום `System.IdentityModel.Tokens.Jwt` (ש-Microsoft מגדירה כ-legacy) משתמשים ב-`Microsoft.IdentityModel.JsonWebTokens` 8.23.0 (`JsonWebTokenHandler.CreateToken`). `BCrypt.Net-Next` 4.2.0.
- `JwtTokenGenerator` מקבל `TimeProvider`, ולכן הרישום `AddSingleton(TimeProvider.System)` הוקדם מ-T16 ל-T12. גם `IPasswordHasher` ו-`IJwtTokenGenerator` נרשמים כבר ב-T12 (Scoped).
- `Jwt:Key` ב-User Secrets חייב להיות באורך 32 תווים לפחות (דרישה של HmacSha256).

### T13 – `AuthService` `[x]`
`IAuthService` ב-`Application/Interfaces`, ו-`AuthService` ב-`Application/Services`.
**חוקים:**
- אימייל מנורמל (Trim ואותיות קטנות) לפני בדיקה ושמירה.
- אימייל קיים: Conflict.
- `TeacherCode` ריק: `Parent`. קוד נכון: `Teacher`. קוד שגוי: Validation.
- התחברות שגויה: Unauthorized, **עם אותה הודעה** לאימייל שגוי ולסיסמה שגויה, כדי לא לגלות אילו אימיילים רשומים.
- לא לרשום ללוג סיסמה, token או את גוף הבקשה.

**החלטות שהתקבלו ב-T13:**
- `Microsoft.Extensions.Logging.Abstractions` ו-`Microsoft.Extensions.Options` (10.0.0) נוספו **במפורש** ל-Application, במקום להגיע בעקיפין דרך AutoMapper. אלה חבילות ממשקים בלבד, בלי HTTP ובלי EF.
- סדר ההרשמה: נרמול → קוד מורה (בלי מסד) → אימייל קיים → hash ושמירה → לוג → token. ההרשמה מחזירה token.
- מקרה קצה ידוע: שתי הרשמות במקביל עם אותו אימייל. האינדקס הייחודי מונע כפילות, והשנייה מחזירה 500 (לא 409). הוחלט להשאיר כך.
- לא נוספו חוקים מעבר לאפיון. הרחבות אפשריות לכיתה: נעילה אחרי ניסיונות כושלים, אישור אימייל, refresh token.

### T14 – אימות, הרשאות, Swagger ו-CORS ב-`Program.cs` `[x]`
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` עם `TokenValidationParameters`: Issuer, Audience, Key, Lifetime. חבילה `Microsoft.AspNetCore.Authentication.JwtBearer` ב-API.
- `AddAuthorization()`.
- **Swagger UI עם כפתור Authorize ל-Bearer** (ראו החלטה פתוחה 7).
- **CORS** לפי `CorsOptions`, כדי שהקליינט יוכל לפנות לשרת.
- `API/Extensions/ClaimsPrincipalExtensions`: `GetUserId()` ו-`GetRole()`. **המשתמשת הנוכחית נקראת תמיד מה-claims של ה-token**, אף פעם לא מגוף הבקשה.

**החלטות שהתקבלו ב-T14:**
- `Swashbuckle.AspNetCore` 10.3.0 ו-`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13. בגרסה 10 של Swashbuckle, `AddSecurityRequirement` מקבל פונקציה עם `document` ו-`OpenApiSecuritySchemeReference` (שונה מהכיתה, 6.6.2).
- `AddOpenApi`/`MapOpenApi` מהתבנית **הוסרו**, וגם החבילה `Microsoft.AspNetCore.OpenApi`. מקור אחד ל-Swagger, כמו ב-Clinic.
- **Swagger פעיל גם ב-Production** בכוונה: פרויקט לימודי, כדי שהמורה ורות יוכלו לבדוק את השרת ב-Render. כל נקודת קצה עדיין דורשת token.
- השרת לא עולה אם `Jwt:Key` חסר או קצר מ-32 תווים (בדיקה בעלייה).
- `ClockSkew` דקה אחת (ברירת המחדל: 5 דקות).
- נוסף `UseAuthentication()` לפני `UseAuthorization()` (בתבנית היה חסר).

### שלב ג – לוגיקה עסקית

### T15 – הרחבת ה-repositories `[x]`
**`ISlotRepository`:**
- `GetForUpdateAsync(id)`: **עם** מעקב, בלי `AsNoTracking`.
- `GetDetailsAsync(id)`: `AsNoTracking`, עם `Include(Teacher)` ו-`Include(Student)`.
- `GetPagedAsync(query)`: סינון לפי מחנכת, פנוי וטווח תאריכים; מיון לפי `StartTimeUtc`; `Skip`/`Take` **בתוך השאילתה**; `CountAsync` נפרד.
- `HasOverlapAsync(teacherId, start, end, excludeId?)`
- `StudentHasBookingAsync(studentId)`: מחליף את `ParentHasBookingForStudentAsync` (החלטה 3: תור אחד לילדה).
- `Remove(slot)`
- `SaveChangesAsync`: עוטף את השמירה ב-`try`, ותופס **רק** `DbUpdateConcurrencyException` ו-`DbUpdateException` שנובעת מהפרה של אינדקס ייחודי (קוד PostgreSQL `23505`). במקרה כזה זורק `SlotConflictException`.

**`IStudentRepository`:**
- `GetWithParentsAsync(id)` (קיים).
- `GetByJoinCodeAsync(code)`: עם מעקב, `Include(Parents)`.
- `GetForTeacherAsync(teacherId)`: `Include(Parents)`.
- `GetForParentAsync(parentId)`: `Users.Where(...).SelectMany(...)` או `Include(u => u.Children).ThenInclude(s => s.HomeroomTeacher)`. **כאן ה-ThenInclude שהמורה דורשת.**
- `JoinCodeExistsAsync`, `AddAsync`, `SaveChangesAsync`.

**`IUserRepository`:** קיים, בלי שינוי.

**טרנזקציה: `IUnitOfWork` (החלטה 4):**
- **Application/Interfaces:** `IUnitOfWork` עם `BeginTransactionAsync(ct)`, שמחזירה `ITransaction`. `ITransaction` יורש מ-`IAsyncDisposable` ויש לו `CommitAsync(ct)`.
- **Infrastructure/Persistence:** `EfUnitOfWork` עוטף את `_context.Database.BeginTransactionAsync`, ו-`EfTransaction` עוטף את `IDbContextTransaction`. אם הטרנזקציה נסגרת בלי `CommitAsync`, היא מתבטלת (Rollback).
- רישום `AddScoped<IUnitOfWork, EfUnitOfWork>`.
- **למה זה עובד עם ה-repositories:** `DbContext` הוא Scoped, ולכן בתוך בקשה אחת כל ה-repositories וה-`EfUnitOfWork` מקבלים **אותו** מופע. הטרנזקציה שנפתחה עליו חלה על כל השאילתות שלהם.
- **למה ממשק ולא `Database.BeginTransactionAsync` ישירות בשירות:** `Database` שייך ל-EF, ו-`Application` לא מכירה EF.

**בעיית N+1:** אף שאילתה לא רצה בתוך לולאה. כל הקשרים נטענים ב-`Include` בשאילתה אחת.

**החלטות שהתקבלו ב-T15:**
- `GetByIdAsync` הוחלף ב-`GetForUpdateAsync` (עם מעקב) וב-`GetDetailsAsync` (בלי מעקב). `ParentHasBookingForStudentAsync` הוחלף ב-`StudentHasBookingAsync`.
- `GetForUpdateAsync` כולל `Include(Teacher)` ו-`Include(Student)`, כדי להחזיר `SlotDto` אחרי שינוי.
- `GetPagedAsync` מקבל `SlotQuery`. מיון משני לפי `Id` (`ThenBy`) לסדר יציב בין עמודים. `FromUtc`/`ToUtc` מסננים לפי `StartTimeUtc`.
- חפיפה: `s.Start < end && start < s.End`. סלוטים צמודים מותרים.
- `SaveChangesAsync`: `catch` של `DbUpdateConcurrencyException` **לפני** `DbUpdateException` (ירושה), ו-`when` עם `PostgresErrorCodes.UniqueViolation`.
- `GetWithParentsAsync`, `GetByJoinCodeAsync` ו-`GetForTeacherAsync` כוללים `Include(HomeroomTeacher)` בשביל `TeacherName`.
- `GetForParentAsync` מתחיל מ-`Users` עם `Include(Children).ThenInclude(HomeroomTeacher)` (דרישת המורה ל-ThenInclude).
- `ITransaction` בלי `RollbackAsync`: יציאה בלי `CommitAsync` מבטלת. `EfTransaction` הוא Adapter.

### T16 – `SlotService`: CRUD וביטול הזמנה `[x]`
`ISlotService` ו-`SlotService`. מזריק `ISlotRepository`, `IMapper`, `ILogger<SlotService>` ו-`TimeProvider` (במקום `DateTime.UtcNow`, כדי שבבדיקות אפשר "לקבוע" את השעה).
- **Create:** רק המחנכת עצמה (`TeacherId` מגיע מה-token). כללים: הסיום אחרי ההתחלה; ההתחלה בעתיד; הזמנים ב-UTC (`DateTimeKind.Utc`), אחרת Validation; אין חפיפה עם סלוט אחר שלה (Conflict).
- **GetPaged:** `pageSize` מוגבל ל-50. מחנכת בלי `teacherId` רואה את הסלוטים שלה. מיפוי ל-`SlotDto` + `BookedByMe` + `StudentName` לפי כללי הפרטיות.
- **GetById:** NotFound אם לא קיים.
- **Update:** מעתיקים את הזמנים ביד (`slot.StartTimeUtc = request.StartTimeUtc!.Value`), בלי AutoMapper (החלטה מ-T11). רק בעלת הסלוט (Forbidden); סלוט תפוס לא משתנה (Conflict); אותם כללי זמנים וחפיפה.
- **Delete:** רק בעלת הסלוט; סלוט תפוס לא נמחק (Conflict).
- **CancelBooking:** רק ההורה שהזמינה (Forbidden); סלוט שעבר לא מתבטל (Validation); מאפסים `BookedByParentId` ו-`StudentId`; `SlotConflictException` מחזירה Conflict.

**החלטות שהתקבלו ב-T16:**
- המשתמשת (`userId`, `role`) מועברת לשירות כפרמטרים מה-Controller.
- סדר בדיקות קבוע: קיים (404) → שלך (403) → מצב (409/400).
- `ValidateTimes` משותפת ליצירה ולעדכון. `DateTimeKind.Utc` נדרש (Npgsql), אחרת 400.
- `ToDto` היא המקום היחיד שבונה `SlotDto` (פרטיות). מספיק להשוות `userId` ל-`TeacherId`/`BookedByParentId`.
- `catch (SlotConflictException)` גם ב-Update, Delete ו-CancelBooking (Version נבדק גם ב-DELETE).
- ביצירה טוענים מחדש עם `GetDetailsAsync`, כי `slot.Teacher` הוא null אחרי שמירה עם `TeacherId` בלבד.
- הורה רואה ברשימה סלוטים של כל המחנכות (בלי שמות ילדות). ההגבלה למחנכת של הילדה היא ב-T17.

### T17 – `BookAsync`: לב הפרויקט `[x]`
לפי הסדר:
0. `await using var tx = await _unitOfWork.BeginTransactionAsync(ct);`. מכאן כל הבדיקות והשמירה נמצאות באותה טרנזקציה.
1. טעינת הסלוט **עם מעקב** (`GetForUpdateAsync`). לא נמצא: NotFound.
2. טעינת התלמידה עם ההורים. לא נמצאה: NotFound.
3. ההורה המחוברת לא ברשימת ההורים של התלמידה: Forbidden.
4. מחנכת הסלוט שונה ממחנכת התלמידה: Forbidden.
5. הסלוט בעבר: Validation.
6. הסלוט כבר תפוס: Conflict.
7. לילדה כבר יש תור: Conflict.
8. הצבת `BookedByParentId` ו-`StudentId`.
9. `SaveChangesAsync` ואחריה `tx.CommitAsync(ct)`, שתיהן בתוך `try`. אם חוזרים בשלב מוקדם (NotFound, Forbidden וכו'), אין Commit, ו-`await using` מבטל את הטרנזקציה. תפיסת `SlotConflictException` (ורק אותה): לוג **Warning** עם מזהה הסלוט ומזהה ההורה, והחזרת Conflict: "הסלוט נתפס על ידי הורה אחרת. נסי סלוט אחר."
10. הצלחה: `SlotDto`.

**איפה התחרות ואיך היא נעצרת (להסבר במבחן):**
- שתי בקשות קוראות את הסלוט כשהוא פנוי, ושתיהן עוברות את שלב 6.
- הראשונה שומרת: `UPDATE ... WHERE Id=@id AND Version=@old`. שורה אחת מתעדכנת, ו-`Version` מתחלף.
- השנייה שולחת את אותו `UPDATE` עם ה-`Version` הישן. 0 שורות מתעדכנות, EF זורק `DbUpdateConcurrencyException`, ה-repository מתרגם אותה ל-`SlotConflictException`, והתשובה היא 409.

**על "אותה טרנזקציה" (המורה כתבה שהבדיקה והשמירה חייבות להיות באותה טרנזקציה):** הבדיקות והשמירה עטופות ב-`IUnitOfWork` (החלטה 4). חשוב לדעת להסביר **מה עוצר בפועל את ההתנגשות** בכל שכבה:
- **הטרנזקציה** מבטיחה "הכול או כלום": אם משהו נכשל באמצע, שום דבר לא נשמר. היא **לא** נועלת את השורה. ברמת הבידוד הרגילה של PostgreSQL (Read Committed), שתי בקשות עדיין יכולות לקרוא את הסלוט כפנוי.
- **`Version`** הוא מה שעוצר את שתי ההורות על אותו סלוט: ה-`UPDATE` של השנייה לא מוצא שורה.
- **האינדקס הייחודי** (T22) הוא מה שעוצר את אבא ואמא על שני סלוטים שונים לאותה ילדה.

שלושת המנגנונים יחד: הטרנזקציה נותנת שלמות, ו-`Version` והאינדקס מזהים את ההתנגשות. זה מה שנכתוב ב-README.

**החלטות שהתקבלו ב-T17:**
- `BookAsync` נמצא ב-`SlotService`, שמקבל עכשיו גם `IStudentRepository` ו-`IUnitOfWork`.
- `try` עוטף גם את `SaveChangesAsync` וגם את `CommitAsync`. כישלון בשמירה מדלג על ה-Commit, והטרנזקציה מתבטלת.
- בסוף טוענים מחדש עם `GetDetailsAsync`, כדי שההורה תקבל את שם הילדה (כמו ב-Create).
- עד T22 (האינדקס הייחודי) נשאר פתח רק במקרה של אבא ואמא על שני סלוטים שונים לאותה ילדה, באותה שנייה בדיוק.

### T18 – `StudentService` `[x]`
- **Create** (Teacher): `HomeroomTeacherId` מגיע מה-token. `JoinCode` נוצר אוטומטית: 8 תווים, בלי תווים מבלבלים כמו 0/O או 1/I. אם הקוד כבר קיים, יוצרים חדש.
- **GetMine:** למחנכת, התלמידות שלה עם `JoinCode` ומספר ההורים. להורה, הילדות שלה עם שם המחנכת, ובלי `JoinCode`.
- **GetById:** רק המחנכת או הורה מקושרת (Forbidden).
- **Link** (Parent): קוד לא קיים: NotFound. כבר מקושרת: Conflict. אחרת מוסיפים את ההורה ל-`Parents`.

**החלטות שהתקבלו ב-T18:**
- הקוד מוגרל עם `RandomNumberGenerator.GetString` (קריפטוגרפי), מתוך `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`, ב-`do...while` עד שאינו תפוס.
- `ToDto` ממלא `JoinCode` ו-`ParentsCount` רק כש-`HomeroomTeacherId == userId`.
- בקישור: הקוד מנורמל ל-`Trim().ToUpperInvariant()`. ההורה נטענת מ-`IUserRepository` (אותו DbContext).
- ביצירה: `Trim` על השם, וטעינה מחדש בשביל שם המחנכת.

### שלב ד – שכבת ה-API

### T19 – Controllers `[x]`
`AuthController`, `SlotsController`, `StudentsController`, `StudentLinksController`.
- `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]` / `[Authorize(Roles = "Teacher")]`.
- `[FromBody]`, `[FromQuery]` (`SlotQuery`) ו-`[FromRoute]` (`id`) במפורש.
- `CancellationToken ct` בכל פעולה, ומועבר לשירות.
- `CreatedAtAction(nameof(GetById), new { id }, dto)` ליצירה: 201 + Location. `NoContent()` למחיקה ולביטול: 204.
- `API/Extensions/ServiceResultExtensions.ToActionResult()`: מתרגם `ErrorType` ל-400, 401, 403, 404 או 409, בגוף אחיד.
- `[ProducesResponseType]` לכל סטטוס, כדי ש-Swagger יתעד הכול.
- **ModelState:** `[ApiController]` מחזיר 400 אוטומטית כשהולידציה נכשלת. מגדירים `InvalidModelStateResponseFactory`, כך שגם שגיאת ולידציה תחזור באותו מבנה JSON של שאר השגיאות.
- **enum כמחרוזת ב-JSON:** ב-`Program.cs` מוסיפים `JsonStringEnumConverter` ל-`AddControllers().AddJsonOptions(...)`, כדי ש-`AuthResponse.Role` יחזור כ-`"Teacher"` ולא כ-`1` (החלטה מ-T10).
- **ה-Controller "רזה":** אין בו לוגיקה עסקית ואין בו `DbContext`. רק קבלת הבקשה, קריאה לשירות ותרגום התוצאה.

**החלטות שהתקבלו ב-T19:**
- כתובות מפורשות (`api/slots`, `api/student-links` וכו') במקום `api/[controller]`, כדי להתאים לטבלת נקודות הקצה.
- גוף שגיאה אחיד: `record ErrorResponse(int Status, string Message)` ב-`API/Models`. גם `InvalidModelStateResponseFactory` מחזיר אותו.
- `ToActionResult` בשתי גרסאות: עם ערך (200) ובלי ערך (204). יצירה מחזירה `CreatedAtAction` (201 + Location).
- `[Authorize(Roles = nameof(Role.Teacher))]`: `nameof` במקום מחרוזת, כדי שטעות כתיב לא תתקמפל.
- `JsonStringEnumConverter` נוסף ב-`AddJsonOptions`.

### T20 – Middleware `[x]`
- **`ExceptionMiddleware`:** תופס חריגה לא צפויה, רושם לוג **Error**, ומחזיר 500 בפורמט `{ status, message, correlationId }`, בלי stack trace.
- **`CorrelationIdMiddleware`:** קורא את `X-Correlation-Id` מהבקשה, או יוצר חדש. שומר אותו ב-`HttpContext.Items`, מחזיר אותו ב-header של התשובה, ופותח `logger.BeginScope` כדי שהמזהה יופיע בכל שורת לוג של הבקשה.
- **`RequestLoggingMiddleware`:** לוג **Information** לכל בקשה: method, path, status, משך זמן. בלי גוף הבקשה.

**הסדר ב-pipeline:** Exception → CorrelationId → RequestLogging → Swagger → HTTPS → CORS → Authentication → Authorization → MapControllers.
**החלטות שהתקבלו ב-T20:**
- שלושת ה-middleware ב-`SlotRace.API/Middleware`. `ErrorResponse` קיבל שדה אופציונלי `CorrelationId` (מתמלא רק ב-500).
- מזהה מהקליינט מתקבל רק אם אינו ריק ועד 64 תווים; אחרת נוצר Guid חדש.
- הכותרת `X-Correlation-Id` נוספת ב-`Response.OnStarting`. `ExceptionMiddleware` קורא את המזהה מ-`HttpContext.Items` וכותב אותו בהודעת הלוג (ה-scope כבר נסגר כשהחריגה מגיעה אליו).
- `RequestLoggingMiddleware` רושם method, path, status וזמן. בבקשה שנכשלה בחריגה אין שורה שלו; `ExceptionMiddleware` רושם Error.

**למה Exception ראשון:** middleware עוטף את כל מה שאחריו, ולכן הראשון תופס חריגות מכולם. **למה Authentication לפני Authorization:** קודם מזהים מי המשתמשת, ורק אחר כך בודקים מה מותר לה.

### T21 – NLog `[x]`
- חבילה `NLog.Web.AspNetCore` ב-API, `builder.Host.UseNLog()` (לפי התיעוד של NLog לגרסה המותקנת).
- `nlog.config`: שני יעדים, קובץ וקונסול. ה-CorrelationId ב-layout. התיקייה `logs/` נמצאת ב-`.gitignore`.
- **רמות:** Debug לפיתוח; Information לבקשות וזרימה רגילה; Warning להתנגשות על סלוט; Error לחריגה.
- **בקוד רק `ILogger<T>`.** NLog מתחבר מתחת, ולכן אפשר להחליף ספריית לוגים בלי לגעת ב-Application. זה אותו רעיון של הפרדת תלויות.
- **לא נרשמים:** סיסמאות, tokens, גוף בקשת הרשמה.

**החלטות שהתקבלו ב-T21:**
- `NLog.Web.AspNetCore` 6.2.2 ב-API בלבד. `builder.Logging.ClearProviders()` ו-`builder.Host.UseNLog()`.
- `nlog.config` ב-`SlotRace.API` (מועתק ל-bin עם `CopyToOutputDirectory`). יעדים: קונסול וקובץ יומי `logs/slotrace-${shortdate}.log` (7 ימים).
- פורמט: תאריך | רמה | `${scopeproperty:item=CorrelationId}` | מחלקה | הודעה | חריגה.
- חוקים: `Microsoft.Hosting.Lifetime` מ-Info; שאר `Microsoft.*`/`System.*` רק מ-Warning; הקוד שלנו מ-Info.
- `BeginScope` ב-`CorrelationIdMiddleware` עבר למערך `KeyValuePair`, כמו בתיעוד של NLog.

### שלב ה – מסד נתונים מוכן להרצה

### T22 – migration שני ו-Seeder `[x]`
- **migration `AddBookingIndexes`:**
  - אינדקס **ייחודי חלקי** על `Slots.StudentId` עם `HasFilter("\"StudentId\" IS NOT NULL")`. כך מסד הנתונים עצמו אוסר שני תורים לאותה ילדה. זה מגן על "המשפט השני" גם כשאבא ואמא מזמינים באותו רגע.
  - אינדקס על `(TeacherId, StartTimeUtc)` לשאילתת הרשימה ולבדיקת החפיפה.
- **`Infrastructure/Persistence/DbSeeder`:** רץ בעלייה ב-`Program.cs`, בתוך `CreateScope()`. מריץ `MigrateAsync()`, ואם הטבלאות ריקות מכניס נתונים:
  - 2 מחנכות (כדי שאפשר להדגים Forbidden אצל מחנכת אחרת).
  - 3 הורים.
  - 4 תלמידות עם `JoinCode` קבוע, וקישורים בין הורים לילדות (כולל ילדה עם שני הורים, להדגמת המשפט השני).
  - כ-12 סלוטים עתידיים.
  - ה-hash של הסיסמאות מחושב בזמן ריצה דרך `IPasswordHasher`.
- **קריטריון סיום:** מוחקים את מסד הנתונים, מריצים את ה-API, והכול עולה בלי שום פקודה ידנית (דרישת המורה).

**החלטות שהתקבלו ב-T22:**
- migration `AddBookingIndexes`: מחליף את אינדקסי ה-FK האוטומטיים על `StudentId` ו-`TeacherId` באינדקס ייחודי חלקי ובאינדקס משולב.
- `DbSeeder` הוא static עם פרמטרים מפורשים (`AppDbContext`, `IPasswordHasher`, `TimeProvider`). נקרא ב-`Program.cs` בתוך `CreateScope()`.
- נתוני דוגמה: סיסמה `Password123!` לכולם. מחנכות: שרה (`sara@slotrace.test`), רבקה (`rivka@...`). הורים: לאה ודוד (שניהם הורים של מירי), רחל. תלמידות וקודים: מירי `SARA2345`, נועה `RVKA2345`, חני `SARA6789`, תמר `RVKA6789` (בלי הורים). 6 סלוטים לכל מחנכת מחר מ-14:00 UTC; הראשון של שרה מוזמן ע"י רחל עבור חני.
- `Add-Migration` רץ עם Default project = `SlotRace.Infrastructure` ו-Startup project = `SlotRace.API`.

### שלב ו – בדיקות

### T23 – בדיקות יחידה (xUnit ו-Moq) `[x]`
פרויקט `SlotRace.Tests` (xUnit), שמפנה ל-`Application` ול-`Domain`. חבילות `Moq` ו-`Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`).
את `IUnitOfWork` ו-`ITransaction` מדמים עם Moq. בהצלחה בודקים ש-`CommitAsync` נקראה, ובדחייה בודקים שהיא **לא** נקראה.
**על `SlotService.BookAsync`:**
- הצלחה: מחזירה `SlotDto`, ו-`SaveChangesAsync` נקראת פעם אחת.
- סלוט תפוס: Conflict, ו-`SaveChangesAsync` **לא** נקראת.
- תלמידה לא מקושרת להורה: Forbidden.
- מחנכת אחרת: Forbidden.
- סלוט בעבר: Validation.
- **ה-repository זורק `SlotConflictException`: Conflict** (המקרה ה"נדחה" של המשאב המוגבל, דרישה 13).

**על `AuthService`:** אימייל קיים מחזיר Conflict; קוד מורה שגוי מחזיר Validation.

**החלטות שהתקבלו ב-T23 (אפשרות ב', 28 בדיקות):**
- הפרויקט נוצר מתבנית xUnit של Visual Studio (xUnit 2.9.3). נוספו `Moq` 4.21.0 ו-`Microsoft.Extensions.TimeProvider.Testing` 10.11.0, והפניות ל-Application ול-Domain.
- `TestHelpers`: "עכשיו" קבוע (20.10.2026 09:00 UTC) עם `FakeTimeProvider`, ו-AutoMapper **אמיתי** עם `MappingProfile` (כך נבדק גם ה-`Ignore`).
- `AuthServiceTests` (7): כולל נרמול אימייל, hash במקום סיסמה, ואותה הודעה בשני כישלוני התחברות.
- `SlotServiceBookTests` (8): כל שבע הבדיקות של `BookAsync`, הצלחה עם `Commit`, ו-`SlotConflictException` בלי `Commit`. מצב תקין בבנאי, ושינוי אחד בכל בדיקה.
- `SlotServiceTests` (7): Create (עבר, לא UTC, חפיפה), Update של מחנכת אחרת, Delete של סלוט תפוס, ופרטיות שם הילדה.
- `StudentServiceTests` (6): פרטיות `JoinCode`, הרשאה, וקישור (כולל נרמול הקוד).

### T24 – בדיקת ההוכחה: שני DbContext `[x]`
המורה בודקת אותה בנפרד.
- שני מופעים נפרדים של `AppDbContext` על אותו מסד נתונים.
- שניהם טוענים את אותו סלוט, ושניהם מציבים הזמנה.
- הראשון שומר בהצלחה.
- השני מקבל `DbUpdateConcurrencyException` (`Assert.ThrowsAsync`).
- בדיקה נוספת: אחרי זה במסד יש הזמנה אחת בלבד.
- **מסד הנתונים לבדיקה:** ראו החלטה פתוחה 5. הפרויקט יפנה גם ל-`Infrastructure`.

**החלטות שהתקבלו ב-T24:**
- SQLite בזיכרון (`Microsoft.EntityFrameworkCore.Sqlite` 10.0.13), חיבור פתוח אחד לכל בדיקה, `EnsureCreated` (לא migrations, שנכתבו ל-PostgreSQL).
- הבדיקות פונות ל-`AppDbContext` ישירות (המנגנון עצמו). התרגום ל-409 נבדק ב-T23.
- בדיקה 1 (הדרישה): שתי הורות, אותו סלוט → `DbUpdateConcurrencyException`; במסד הזמנה אחת.
- בדיקה 2 (תוספת): אבא ואמא, שני סלוטים, אותה ילדה → `DbUpdateException` מהאינדקס הייחודי; למירי תור אחד.

### שלב ז – סיום השרת

### T25 – README ובדיקה סופית `[x]`
**README (בפרויקט השרת):**
- תיאור המערכת בפסקה.
- **פסקה על המשאב המוגבל:** מה המשאב, איפה התחרות (שתי ההורות; אבא ואמא), ואיך המערכת מגיבה (409 והודעה).
- הוראות הרצה מקומית: Docker עם `postgres:16`, `dotnet user-secrets set` לשלושת הסודות, `dotnet run` (ה-migrations וה-seed רצים לבד).
- משתמשות דמו לכל role, עם אימייל וסיסמה.
- קוד מורה לדמו: רק בהוראות ההרצה, כערך שכל אחת מגדירה לעצמה ב-User Secrets.
- קישור ל-Swagger ולשרת ב-Render.

**בדיקה סופית:** `git grep` על סודות; clone נקי למחשב, הרצה מאפס ובדיקה שהכול עולה; כל נקודות הקצה עובדות ב-Swagger; בדיקות ירוקות.

**החלטות שהתקבלו ב-T25:**
- **שם המוצר למשתמשות: SchoolSlot.** `SlotRace` נשאר שם הקוד של הפרויקט (השרת, ה-namespaces וה-repository), כי הוא מתאר את האתגר הטכני. הקליינט (T26) מציג רק "SchoolSlot".
- קוד המורה: בהרצה מקומית כל אחת מגדירה קוד משלה; ב-Render הקוד נמסר בנפרד עם ההגשה (לא ב-GitHub). לבדיקה רגילה יש מחנכות דמו.
- הבדיקה הסופית: `git grep` נקי מסודות ✓; 30 בדיקות ירוקות ✓; הרצה מאפס (`Drop-Database` ו-F5) ✓; נקודות הקצה נבדקו ב-Swagger ✓.
- הקישורים ל-Render ולקליינט ב-README יושלמו ב-T26–T27.

### שלב ח – קליינט והעלאה

### T26 – קליינט React (repository נפרד: `SchoolSlot-Client`) `[x]`
React עם Vite. ייכתב בעזרת AI, במינימום שמדגים את השרת: בכיתות הסמינר הקליינט לא נבדק (החלטה 1). `.gitignore` עם `node_modules`. כתובת השרת ב-`VITE_API_URL`.
**ארבעה מסכים (דרישת המורה):**
1. התחברות והרשמה: שמירת ה-JWT ושליחתו ב-header `Authorization: Bearer`.
2. רשימת סלוטים עם pagination אמיתי מול השרת (`page` ו-`pageSize` ב-query).
3. כפתור "קביעת תור" (בחירת ילדה, ואז `POST /booking`).
4. **הצגה ברורה של 409**: "הסלוט נתפס על ידי הורה אחרת". זה המסך היחיד שהמורה אמרה שבאמת חשוב.

ואם יש זמן: מסך מחנכת ליצירת סלוטים ותלמידות, ומסך "קישור ילדה בקוד".

**החלטות שהתקבלו ב-T26:**
- ה-repository נקרא `SchoolSlot-Client` (שם המוצר), בתיקייה `hw\SchoolSlot-Client`, לצד השרת.
- Vite + React 19 + TypeScript, בלי ספריות נוספות (בלי router; לשוניות ב-state). עברית מימין לשמאל, גופן Rubik, עיצוב בהשראת tor.digital בלי תמונות.
- כל ארבעת המסכים, וגם מסך מחנכת (זמנים ותלמידות עם קודים) וקישור ילדה בקוד. 409 מוצג בכרטיס אדום בולט עם הודעת השרת.
- ה-token נשמר ב-localStorage; 401 מחזיר למסך ההתחברות. זמנים נשלחים לשרת כ-UTC (`toISOString`) ומוצגים בשעון המקומי.
- `VITE_API_URL` (ברירת מחדל `https://localhost:7256`), ו-`.env.example` לדוגמה.

### T27 – העלאה ל-Render `[ ]`
- **שרת:** `Dockerfile` רב-שלבי בשורש ה-solution (build עם `sdk:10.0` והרצה עם `aspnet:10.0`). Web Service ב-Render מסוג Docker, מחובר ל-`SlotRace`.
- **מסד נתונים:** PostgreSQL של Render. ה-connection string נכתב בפורמט `Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`, כי Npgsql לא קורא את פורמט ה-URL שמוצג ב-Render.
- **משתני סביבה ב-Render:** `ConnectionStrings__Default`, `Jwt__Key`, `Registration__TeacherCode`, `Cors__AllowedOrigins__0`. הקו הכפול `__` מחליף את `:` של ההגדרות.
- **קליינט:** Static Site ב-Render, עם `VITE_API_URL` לכתובת השרת.
- ה-migrations וה-seed רצים לבד בעלייה (T22).

**לא בטוחה בפרטים הבאים של Render, ולבדוק בתיעוד בזמן הביצוע:** איזה פורט השרת צריך להאזין לו, ולכמה זמן מסד נתונים חינמי נשמר.

**נבדק בתיעוד של Render (T27):** הפורט מגיע במשתנה `PORT` (ברירת מחדל 10000), וה-Dockerfile קורא אותו. שרת חינמי "נרדם" אחרי 15 דקות בלי תנועה, והתעוררות לוקחת כדקה. מסד PostgreSQL חינמי פג אחרי 30 יום (ועוד 14 יום חסד).

---

## טבלת כיסוי: דרישות המורה ↔ משימות

| # | דרישה | איפה |
|---|---|---|
| 1 | REST, סטטוסים (200/201+Location/204/400/401/403/404/409), Swagger | טבלת נקודות קצה, T14, T19 |
| 2 | `FromRoute`/`FromQuery`/`FromBody`, Data Annotations, ModelState, ולידציה עסקית בשירות | T10, T16–T19 |
| 3 | ארבע שכבות וכיוון תלויות; Core בלי EF; Controller בלי DbContext | מיפוי שכבות, T1, T8, T15, T19 |
| 4 | DI דרך constructor, ממשקים, רישום ב-Program, lifetimes | T6, T9, T12–T18; שאלות הכנה |
| 5 | Code-First, DbSet, לפחות 2 migrations, connection string מהקונפיגורציה, Seed | T3, T4, T22 |
| 6 | CRUD מלא, LINQ עם סינון ומיון, `Skip`/`Take` במסד, `AsNoTracking` | T15, T16 |
| 7 | one-to-many, many-to-many, `Include`/`ThenInclude`, Fluent API, בלי N+1 | T3, T15 |
| 8 | DTOs נפרדים ל-request ול-response, AutoMapper Profiles | T10, T11 |
| 9 | async עד ה-DbContext, `CancellationToken`, בלי `Result`/`Wait` | כל המשימות |
| 10 | Middleware לשגיאות (JSON אחיד), CorrelationId בלוגים, סדר pipeline | T20 |
| 11 | appsettings + Development, `IOptions`, אין סודות ב-repo, User Secrets | T0, T9, T25 |
| 12 | הרשמה, התחברות, JWT עם claims, 2 roles, `[Authorize]`, הרשאה בשרת | T12–T14, T19 |
| 13 | xUnit + Moq על השירות, מקרה תקין ומקרה נדחה של המשאב | T23 |
| ג | concurrency token, 409 עם הודעה, בדיקת שני DbContext, פסקה ב-README | T3, T17, T24, T25 |
| ד | NLog, CorrelationId, Error/Warning/Information, בלי מידע רגיש | T20, T21 |
| ה | קליינט: התחברות, רשימה עם עימוד, פעולה על המשאב, הצגת 409 | T26 |
| ו | שני repositories, `.gitignore`, README, משתמשות דמו, עלייה בלי התערבות ידנית | T0, T22, T25, T26 |
| ז+ח | Render, PostgreSQL, UTC | T4, T27 |

---

## החלטות שרות קיבלה (15.10.2026)

1. **כיתה: סמינר.** הקליינט לא נבדק. T26 ייבנה במינימום שמדגים את השרת, כולל הצגת 409.
2. **Repository: Public.** אין בו סודות, והמורה ו-Render ניגשות אליו בלי הגדרות נוספות.
3. **תור אחד לילדה** (לא להורה-לילדה). `StudentHasBookingAsync` + אינדקס ייחודי חלקי (T22). גם אבא ואמא לא יקבעו שני תורים לאותה ילדה.
4. **טרנזקציה מפורשת סביב ההזמנה: כן, דרך `IUnitOfWork`** (T15, T17). זה עונה מילולית על המשפט של המורה, "הבדיקה העסקית והשמירה חייבות להיות באותה טרנזקציה".

## החלטות פתוחות

5. ~~**מסד הנתונים לבדיקת שני ה-DbContext**~~ **נסגרה ב-T24: SQLite בזיכרון.** הנוסח הקודם: **ההמלצה: SQLite in-memory** (חבילה `Microsoft.EntityFrameworkCore.Sqlite` בפרויקט הבדיקות). הוא מהיר ולא צריך Docker, ו-concurrency token מסוג Guid עובד בו כמו ב-PostgreSQL. החלופה היא PostgreSQL אמיתי ב-Docker: נאמן יותר, אבל הבדיקה תיכשל כשהקונטיינר כבוי.
6. ~~**גרסת AutoMapper**~~ **נסגרה ב-T11:** 16.2.0 בלי מפתח, כמו בכיתה. מגרסה 15 יש רישיון כפול, והגרסה החינמית (Community) כוללת שימוש לימודי. בלי מפתח הספרייה עובדת, ורק מופיעה הודעה בלוג.
7. ~~**Swagger ב-.NET 10**~~ **נסגרה ב-T14:** Swashbuckle 10.3.0, פעיל גם ב-Render. הנוסח הקודם: כרגע בפרויקט יש רק `AddOpenApi` המובנה, שמייצר את המסמך אבל לא את המסך. המורה דורשת "Swagger פעיל". **ההמלצה: Swashbuckle (Swagger UI).** לבדוק בזמן ההתקנה גרסה שתואמת ל-.NET 10, כי הגדרת ה-Bearer השתנתה בין הגרסאות. בנוסף צריך להחליט אם Swagger פעיל גם ב-Render. ההמלצה: כן, כדי שאפשר יהיה לבדוק את השרת.
8. **`/booking` ביחיד:** ההסבר נמצא בטבלת נקודות הקצה. אם המורה מקפידה על רבים בכל מקום, החלופה היא `/api/slots/{id}/bookings`.
9. **תאריך ההגשה החדש:** לא ידוע.

---

## הכנה למבחן: שאלות שכדאי לדעת לענות עליהן

- למה הממשקים ב-Application? (היפוך תלויות; ראו "מיפוי השכבות".)
- למה `DbContext` הוא Scoped, ולמה Singleton שמחזיק Scoped הוא באג? (ה-Singleton חי כל זמן שהאפליקציה רצה, ולכן "תופס" `DbContext` אחד שמשותף לכל הבקשות. `DbContext` לא בטוח לשימוש מכמה בקשות במקביל, וה-ChangeTracker שלו מתמלא בלי סוף. בפיתוח ASP.NET Core מזהה את זה וזורק שגיאה.)
- מה ההבדל בין optimistic ל-pessimistic, ולמה בחרנו optimistic?
- מה בדיוק קורה ב-SQL כשהבקשה השנייה שומרת? (`WHERE Version=@old`, 0 שורות, חריגה.)
- למה מחליפים את `Version` ב-`SaveChangesAsync` ולא בשירות?
- למה בטעינת סלוט לשינוי אסור `AsNoTracking`?
- למה 409 ולא 400 או 500?
- מה זה N+1, ואיך `Include` פותר אותה?
- למה `Skip`/`Take` לפני `ToListAsync` ולא אחרי?
- למה `ExceptionMiddleware` ראשון ב-pipeline?
- מה ההבדל בין ולידציה ב-Data Annotations לוולידציה עסקית?
- למה אסור `.Result` ו-`.Wait()`?
- למה NLog לא מופיע ב-Application?
- למה `DateTime.UtcNow` ולא `DateTime.Now` ב-PostgreSQL?
