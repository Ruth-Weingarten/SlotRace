using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Application.Interfaces;
using SlotRace.Application.Services;
using SlotRace.Domain.Entities;

namespace SlotRace.Tests;

// בדיקות לשאר הפעולות של SlotService (T16): בדיקה אחת מכל סוג שגיאה, ובדיקות פרטיות
public class SlotServiceTests
{
    private const int TeacherId = 1;
    private const int ParentId = 5;

    private readonly Mock<ISlotRepository> _slots = new();
    private readonly SlotService _service;
    private readonly DateTime _now = TestHelpers.Now.UtcDateTime;

    public SlotServiceTests()
    {
        _service = new SlotService(
            _slots.Object,
            Mock.Of<IStudentRepository>(),   // לא בשימוש בפעולות האלה
            Mock.Of<IUnitOfWork>(),
            TestHelpers.CreateMapper(),
            TestHelpers.CreateClock(),
            NullLogger<SlotService>.Instance);
    }

    private Slot BookedSlot() => new()
    {
        Id = 42,
        TeacherId = TeacherId,
        Teacher = new User { Id = TeacherId, FullName = "שרה כהן" },
        StartTimeUtc = _now.AddDays(1),
        EndTimeUtc = _now.AddDays(1).AddMinutes(15),
        BookedByParentId = ParentId,
        StudentId = 7,
        Student = new Student { Id = 7, FullName = "מירי אברהם" }
    };

    // ---------- Create: ולידציה עסקית ----------

    [Fact]
    public async Task CreateAsync_StartInPast_ReturnsValidation()
    {
        var request = new CreateSlotRequest { StartTimeUtc = _now.AddHours(-1), EndTimeUtc = _now.AddMinutes(-45) };

        var result = await _service.CreateAsync(request, TeacherId, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_NonUtcTimes_ReturnsValidation()
    {
        // בלי Z: DateTimeKind.Unspecified. לא ברור אם זו שעה בישראל או בלונדון
        var start = DateTime.SpecifyKind(_now.AddDays(1), DateTimeKind.Unspecified);
        var request = new CreateSlotRequest { StartTimeUtc = start, EndTimeUtc = start.AddMinutes(15) };

        var result = await _service.CreateAsync(request, TeacherId, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_OverlappingSlot_ReturnsConflict_AndDoesNotAdd()
    {
        _slots.Setup(r => r.HasOverlapAsync(TeacherId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);
        var request = new CreateSlotRequest { StartTimeUtc = _now.AddDays(1), EndTimeUtc = _now.AddDays(1).AddMinutes(15) };

        var result = await _service.CreateAsync(request, TeacherId, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error);
        _slots.Verify(r => r.AddAsync(It.IsAny<Slot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Update / Delete: בעלות ומצב ----------

    [Fact]
    public async Task UpdateAsync_SlotOfAnotherTeacher_ReturnsForbidden()
    {
        var slot = BookedSlot();
        slot.TeacherId = 2;
        slot.BookedByParentId = null;
        _slots.Setup(r => r.GetForUpdateAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        var request = new UpdateSlotRequest { StartTimeUtc = _now.AddDays(2), EndTimeUtc = _now.AddDays(2).AddMinutes(15) };

        var result = await _service.UpdateAsync(42, request, TeacherId, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.Error);
        _slots.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_BookedSlot_ReturnsConflict_AndDoesNotRemove()
    {
        _slots.Setup(r => r.GetForUpdateAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(BookedSlot());

        var result = await _service.DeleteAsync(42, TeacherId, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error);
        _slots.Verify(r => r.Remove(It.IsAny<Slot>()), Times.Never);
    }

    // ---------- פרטיות (ToDto) ----------

    [Fact]
    public async Task GetByIdAsync_OtherParent_DoesNotSeeStudentName()
    {
        _slots.Setup(r => r.GetDetailsAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(BookedSlot());

        var result = await _service.GetByIdAsync(42, userId: 6, CancellationToken.None);   // הורה אחרת

        Assert.True(result.Value!.IsBooked);          // רואה שהסלוט תפוס
        Assert.False(result.Value.BookedByMe);
        Assert.Null(result.Value.StudentName);        // אבל לא את שם הילדה
    }

    [Fact]
    public async Task GetByIdAsync_BookingParentAndTeacher_SeeStudentName()
    {
        _slots.Setup(r => r.GetDetailsAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(BookedSlot());

        var asParent = await _service.GetByIdAsync(42, ParentId, CancellationToken.None);
        var asTeacher = await _service.GetByIdAsync(42, TeacherId, CancellationToken.None);

        Assert.Equal("מירי אברהם", asParent.Value!.StudentName);
        Assert.True(asParent.Value.BookedByMe);
        Assert.Equal("מירי אברהם", asTeacher.Value!.StudentName);
    }
}
