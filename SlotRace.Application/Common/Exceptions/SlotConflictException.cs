namespace SlotRace.Application.Common.Exceptions;

// "החריגה שלנו" להתנגשות על סלוט.
// ה-repository (ב-Infrastructure) תופס את החריגה של EF וזורק במקומה את זו,
// וכך Application לא צריכה להכיר את EF.
public class SlotConflictException : Exception
{
    public SlotConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
