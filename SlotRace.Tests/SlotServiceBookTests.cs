using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlotRace.Application.Common;
using SlotRace.Application.Common.Exceptions;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Application.Interfaces;
using SlotRace.Application.Services;
using SlotRace.Domain.Entities;

namespace SlotRace.Tests;

// בדיקות ל-BookAsync (T17), לב הפרויקט. כל בדיקה עוברת על אחת משבע הבדיקות בשירות
public class SlotServiceBookTests
{
    private const int TeacherId = 1;
    private const int ParentId = 5;
    private const int StudentId = 7;
    private const int SlotId = 42;

    private readonly Mock<ISlotRepository> _slots = new();
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITransaction> _transaction = new();
    private readonly Slot _slot;
    private readonly Student _student;
    private readonly SlotService _service;

    // ברירת המחדל לכל בדיקה: "מצב תקין". כל בדיקה משנה רק דבר אחד, כדי לבדוק חוק אחד
    public SlotServiceBookTests()
    {
        var now = TestHelpers.Now.UtcDateTime;

        _slot = new Slot
        {
            Id = SlotId,
            TeacherId = TeacherId,
            Teacher = new User { Id = TeacherId, FullName = "שרה כהן" },
            StartTimeUtc = now.AddDays(1),                 // מחר: בעתיד
            EndTimeUtc = now.AddDays(1).AddMinutes(15)     // פנוי (BookedByParentId = null)
        };

        _student = new Student
        {
            Id = StudentId,
            FullName = "מירי אברהם",
            HomeroomTeacherId = TeacherId,                 // אותה מחנכת כמו של הסלוט
            Parents = { new User { Id = ParentId } }       // ההורה המחוברת היא הורה של מירי
        };

        _slots.Setup(r => r.GetForUpdateAsync(SlotId, It.IsAny<CancellationToken>())).ReturnsAsync(_slot);
        _slots.Setup(r => r.GetDetailsAsync(SlotId, It.IsAny<CancellationToken>())).ReturnsAsync(_slot);
        _students.Setup(r => r.GetWithParentsAsync(StudentId, It.IsAny<CancellationToken>())).ReturnsAsync(_student);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_transaction.Object);
        // StudentHasBookingAsync בלי Setup: השחקן מחזיר false ("למירי אין תור")

        _service = new SlotService(
            _slots.Object,
            _students.Object,
            _unitOfWork.Object,
            TestHelpers.CreateMapper(),
            TestHelpers.CreateClock(),
            NullLogger<SlotService>.Instance);
    }

    private Task<ServiceResult<SlotDto>> Book() =>
        _service.BookAsync(SlotId, new BookSlotRequest { StudentId = StudentId }, ParentId, CancellationToken.None);

    // בכל דחייה: לא נשמר כלום, והטרנזקציה לא אושרה
    private void VerifyNothingSaved()
    {
        _slots.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BookAsync_ValidRequest_SavesAndCommits()
    {
        var result = await Book();

        Assert.True(result.IsSuccess);
        Assert.Equal(ParentId, _slot.BookedByParentId);
        Assert.Equal(StudentId, _slot.StudentId);
        Assert.True(result.Value!.BookedByMe);
        _slots.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookAsync_SlotNotFound_ReturnsNotFound()
    {
        _slots.Setup(r => r.GetForUpdateAsync(SlotId, It.IsAny<CancellationToken>())).ReturnsAsync((Slot?)null);

        var result = await Book();

        Assert.Equal(ErrorType.NotFound, result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task BookAsync_StudentNotLinkedToParent_ReturnsForbidden()
    {
        _student.Parents.Clear();                          // מירי לא הבת של ההורה המחוברת

        var result = await Book();

        Assert.Equal(ErrorType.Forbidden, result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task BookAsync_SlotOfAnotherTeacher_ReturnsForbidden()
    {
        _slot.TeacherId = 2;                               // הסלוט של מחנכת אחרת

        var result = await Book();

        Assert.Equal(ErrorType.Forbidden, result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task BookAsync_SlotInPast_ReturnsValidation()
    {
        _slot.StartTimeUtc = TestHelpers.Now.UtcDateTime.AddHours(-1);   // לפני שעה

        var result = await Book();

        Assert.Equal(ErrorType.Validation, result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task BookAsync_SlotAlreadyBooked_ReturnsConflict()
    {
        _slot.BookedByParentId = 99;                       // הורה אחרת כבר הזמינה

        var result = await Book();

        Assert.Equal(ErrorType.Conflict, result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task BookAsync_StudentAlreadyHasBooking_ReturnsConflict()
    {
        // ההורה השנייה (למשל האבא) כבר הזמין למירי תור בסלוט אחר
        _slots.Setup(r => r.StudentHasBookingAsync(StudentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Book();

        Assert.Equal(ErrorType.Conflict, result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task BookAsync_RepositoryThrowsSlotConflict_ReturnsConflict_AndDoesNotCommit()
    {
        // "ההורה השנייה הפסידה בתחרות": כל הבדיקות עברו, אבל בשמירה ה-Version כבר השתנה
        _slots.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
              .ThrowsAsync(new SlotConflictException("הסלוט שונה על ידי בקשה אחרת."));

        var result = await Book();

        Assert.Equal(ErrorType.Conflict, result.Error);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
