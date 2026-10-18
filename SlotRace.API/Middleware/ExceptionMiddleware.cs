using SlotRace.API.Models;

namespace SlotRace.API.Middleware;

// רשת ביטחון: ראשון בשרשרת, ולכן כל השרת רץ "בתוך" ה-try שלו.
// שגיאות צפויות מטופלות ב-ServiceResult. כאן נתפסות רק הלא צפויות (באג, מסד שנפל)
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;   // התחנה הבאה בשרשרת
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string;

            // ללוג: כל הפרטים, כולל stack trace
            _logger.LogError(ex, "Unhandled exception. CorrelationId {CorrelationId}", correlationId);

            // לקליינט: 500 במבנה האחיד, בלי פרטים פנימיים של הקוד
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(
                StatusCodes.Status500InternalServerError,
                "אירעה שגיאה בלתי צפויה. נסי שוב מאוחר יותר.",
                correlationId));
        }
    }
}
