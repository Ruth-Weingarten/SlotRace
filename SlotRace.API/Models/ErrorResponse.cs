namespace SlotRace.API.Models;

// הגוף של כל שגיאה במערכת, באותו מבנה: { "status": 409, "message": "..." }
// כך הקליינט קורא כל שגיאה באותה דרך
// CorrelationId: רק בשגיאת 500 (ExceptionMiddleware). בשאר השגיאות null
public record ErrorResponse(int Status, string Message, string? CorrelationId = null);
