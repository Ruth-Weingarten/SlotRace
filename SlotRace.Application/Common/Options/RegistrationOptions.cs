namespace SlotRace.Application.Common.Options;

// ההגדרות של ההרשמה, מתוך החלק "Registration" בקונפיגורציה.
// TeacherCode הוא סוד, ולכן נמצא רק ב-User Secrets.
public class RegistrationOptions
{
    public const string SectionName = "Registration";

    public string TeacherCode { get; set; } = string.Empty;
}
