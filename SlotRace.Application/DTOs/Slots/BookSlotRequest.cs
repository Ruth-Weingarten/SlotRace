using System.ComponentModel.DataAnnotations;

namespace SlotRace.Application.DTOs.Slots;

public class BookSlotRequest
{
    // Range ולא Required: ל-int תמיד יש ערך (0), ולכן Range מ-1 תופס "לא נשלח"
    // ההורה לא כאן: היא נלקחת תמיד מה-token
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
}
