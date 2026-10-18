namespace SlotRace.API.Models;

// הגוף של כל שגיאה במערכת, באותו מבנה: { "status": 409, "message": "..." }
// כך הקליינט קורא כל שגיאה באותה דרך
public record ErrorResponse(int Status, string Message);
