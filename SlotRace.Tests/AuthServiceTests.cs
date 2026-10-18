using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SlotRace.Application.Common;
using SlotRace.Application.Common.Options;
using SlotRace.Application.DTOs.Auth;
using SlotRace.Application.Interfaces;
using SlotRace.Application.Services;
using SlotRace.Domain.Entities;
using SlotRace.Domain.Enums;

namespace SlotRace.Tests;

public class AuthServiceTests
{
    private const string TeacherCode = "SECRET-CODE";

    // "השחקנים": Mock לכל תלות של AuthService
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokens = new();
    private readonly AuthService _service;

    // xUnit יוצר מופע חדש של המחלקה לכל בדיקה, ולכן כל בדיקה מתחילה עם שחקנים "נקיים"
    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _tokens.Setup(t => t.Generate(It.IsAny<User>()))
               .Returns(("fake-token", TestHelpers.Now.UtcDateTime.AddHours(1)));

        // הזרקת תלויות ידנית: במקום ה-container, אנחנו מכניסות את השחקנים
        _service = new AuthService(
            _users.Object,
            _hasher.Object,
            _tokens.Object,
            Options.Create(new RegistrationOptions { TeacherCode = TeacherCode }),
            NullLogger<AuthService>.Instance);
    }

    private static RegisterRequest Request(string email = "leah@mail.com", string? teacherCode = null) =>
        new() { FullName = "לאה אברהם", Email = email, Password = "12345678", TeacherCode = teacherCode };

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsConflict()
    {
        // Arrange: התסריט "האימייל כבר קיים"
        _users.Setup(r => r.EmailExistsAsync("leah@mail.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        // Act
        var result = await _service.RegisterAsync(Request(), CancellationToken.None);

        // Assert: 409, ולא נשמרה משתמשת
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WrongTeacherCode_ReturnsValidation_WithoutQueryingDatabase()
    {
        var result = await _service.RegisterAsync(Request(teacherCode: "WRONG"), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.Error);
        // קוד המורה נבדק לפני המסד, ולכן לא נשלחה שאילתה
        _users.Verify(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_CorrectTeacherCode_CreatesTeacher()
    {
        User? saved = null;
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Callback<User, CancellationToken>((user, _) => saved = user)   // "תופסים" את המשתמשת שנשמרה
              .Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(Request(teacherCode: TeacherCode), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Role.Teacher, saved!.Role);
        Assert.Equal(Role.Teacher, result.Value!.Role);
    }

    [Fact]
    public async Task RegisterAsync_NoTeacherCode_CreatesParent_WithNormalizedEmailAndHashedPassword()
    {
        User? saved = null;
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Callback<User, CancellationToken>((user, _) => saved = user)
              .Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(Request(email: "  Leah@Mail.COM "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Role.Parent, saved!.Role);
        Assert.Equal("leah@mail.com", saved.Email);              // נרמול
        Assert.Equal("hashed-password", saved.PasswordHash);     // נשמר ה-hash, לא הסיסמה
        Assert.Equal("fake-token", result.Value!.Token);         // מחוברת מיד אחרי ההרשמה
        _users.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsUnauthorized()
    {
        // אין Setup ל-GetByEmailAsync, ולכן השחקן מחזיר null: "אין משתמשת כזאת"
        var result = await _service.LoginAsync(
            new LoginRequest { Email = "nobody@mail.com", Password = "12345678" }, CancellationToken.None);

        Assert.Equal(ErrorType.Unauthorized, result.Error);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsSameMessageAsUnknownEmail()
    {
        _users.Setup(r => r.GetByEmailAsync("leah@mail.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new User { Id = 5, Email = "leah@mail.com", PasswordHash = "hashed-password" });
        _hasher.Setup(h => h.Verify("wrong-password", "hashed-password")).Returns(false);

        var wrongPassword = await _service.LoginAsync(
            new LoginRequest { Email = "leah@mail.com", Password = "wrong-password" }, CancellationToken.None);
        var unknownEmail = await _service.LoginAsync(
            new LoginRequest { Email = "nobody@mail.com", Password = "12345678" }, CancellationToken.None);

        // אותה תשובה בדיוק, כדי שאי אפשר יהיה לגלות אילו אימיילים רשומים
        Assert.Equal(ErrorType.Unauthorized, wrongPassword.Error);
        Assert.Equal(unknownEmail.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task LoginAsync_CorrectPassword_ReturnsToken()
    {
        _users.Setup(r => r.GetByEmailAsync("leah@mail.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new User { Id = 5, FullName = "לאה אברהם", Email = "leah@mail.com", PasswordHash = "hashed-password", Role = Role.Parent });
        _hasher.Setup(h => h.Verify("12345678", "hashed-password")).Returns(true);

        var result = await _service.LoginAsync(
            new LoginRequest { Email = "Leah@Mail.com", Password = "12345678" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-token", result.Value!.Token);
        Assert.Equal(Role.Parent, result.Value.Role);
    }
}
