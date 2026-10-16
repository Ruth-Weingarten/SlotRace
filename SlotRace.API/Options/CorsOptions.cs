namespace SlotRace.API.Options;

// הכתובות של הקליינט שמותר להן לפנות לשרת, מתוך החלק "Cors" בקונפיגורציה.
// יושב ב-API ולא ב-Application, כי CORS הוא עניין של HTTP בלבד.
public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
