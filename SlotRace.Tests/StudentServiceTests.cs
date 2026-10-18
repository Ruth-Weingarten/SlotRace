using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Students;
using SlotRace.Application.Interfaces;
using SlotRace.Application.Services;
using SlotRace.Domain.Entities;

namespace SlotRace.Tests;

// בדיקות ל-StudentService (T18): פרטיות של JoinCode, הרשאה וקישור הורה
public class StudentServiceTests
{
    private const int TeacherId = 1;
    private const int ParentId = 5;
    private const string JoinCode = "SARA2345";

    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Student _student;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _student = new Student
        {
            Id = 7,
            FullName = "מירי אברהם",
            JoinCode = JoinCode,
            HomeroomTeacherId = TeacherId,
            HomeroomTeacher = new User { Id = TeacherId, FullName = "שרה כהן" },
            Parents = { new User { Id = ParentId } }
        };

        _students.Setup(r => r.GetWithParentsAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(_student);
        _students.Setup(r => r.GetByJoinCodeAsync(JoinCode, It.IsAny<CancellationToken>())).ReturnsAsync(_student);

        _service = new StudentService(
            _students.Object,
            _users.Object,
            TestHelpers.CreateMapper(),
            NullLogger<StudentService>.Instance);
    }

    // ---------- פרטיות והרשאה ----------

    [Fact]
    public async Task GetByIdAsync_Teacher_SeesJoinCodeAndParentsCount()
    {
        var result = await _service.GetByIdAsync(7, TeacherId, CancellationToken.None);

        Assert.Equal(JoinCode, result.Value!.JoinCode);
        Assert.Equal(1, result.Value.ParentsCount);
    }

    [Fact]
    public async Task GetByIdAsync_LinkedParent_DoesNotSeeJoinCode()
    {
        var result = await _service.GetByIdAsync(7, ParentId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("שרה כהן", result.Value!.TeacherName);
        Assert.Null(result.Value.JoinCode);           // secure by default
        Assert.Null(result.Value.ParentsCount);
    }

    [Fact]
    public async Task GetByIdAsync_UnrelatedUser_ReturnsForbidden()
    {
        var result = await _service.GetByIdAsync(7, userId: 99, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.Error);
    }

    // ---------- קישור הורה ----------

    [Fact]
    public async Task LinkAsync_UnknownCode_ReturnsNotFound()
    {
        var result = await _service.LinkAsync(new LinkStudentRequest { JoinCode = "ZZZZ9999" }, ParentId, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    [Fact]
    public async Task LinkAsync_LowercaseCodeOfAlreadyLinkedParent_ReturnsConflict()
    {
        // קוד באותיות קטנות: אם הנרמול לא עובד, השחקן לא יזהה אותו ויחזור NotFound
        var result = await _service.LinkAsync(new LinkStudentRequest { JoinCode = "sara2345" }, ParentId, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error);
        _students.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkAsync_NewParent_AddsParentAndSaves()
    {
        var newParent = new User { Id = 9, FullName = "דוד אברהם" };
        _users.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(newParent);

        var result = await _service.LinkAsync(new LinkStudentRequest { JoinCode = JoinCode }, 9, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(newParent, _student.Parents);                 // נוסף לרשימה (EF יכתוב ל-ParentStudent)
        Assert.Null(result.Value!.JoinCode);                          // הורה לא רואה את הקוד
        _students.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
