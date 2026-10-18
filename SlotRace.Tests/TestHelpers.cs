using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SlotRace.Application.Mapping;

namespace SlotRace.Tests;

internal static class TestHelpers
{
    // "עכשיו" הקבוע של כל הבדיקות: 20.10.2026 בשעה 09:00 UTC.
    // בזכות TimeProvider, הבדיקות יעבדו אותו דבר היום ובעוד שנה
    public static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider CreateClock() => new(Now);

    // מיפוי אמיתי (לא Mock): אותו MappingProfile של האפליקציה, כדי לבדוק גם אותו
    public static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
}
