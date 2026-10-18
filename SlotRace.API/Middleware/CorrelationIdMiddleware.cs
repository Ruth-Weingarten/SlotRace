namespace SlotRace.API.Middleware;

// מזהה לכל בקשה: מופיע בכל שורת לוג של הבקשה, בכותרת התשובה, ובגוף של שגיאת 500.
// כך אפשר למצוא בלוג את כל מה שקרה בבקשה אחת
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // הקליינט שלח מזהה? משתמשים בו. לא שלח, או שהוא ארוך מדי (ניסיון לזהם את הלוג)? יוצרים חדש
        var id = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
            id = Guid.NewGuid().ToString();

        context.Items[ItemKey] = id;   // שומרים ב"תיק" של הבקשה, בשביל ExceptionMiddleware

        // אי אפשר לשנות כותרות אחרי שהתשובה התחילה לצאת, ולכן מוסיפים "רגע לפני"
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });

        // כל שורת לוג שתיכתב בבקשה הזאת תכלול את המזהה
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await _next(context);
        }
    }
}
