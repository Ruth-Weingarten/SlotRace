using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotRace.API.Extensions;
using SlotRace.API.Models;
using SlotRace.Application.DTOs.Students;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Enums;

namespace SlotRace.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _students;

    public StudentsController(IStudentService students)
    {
        _students = students;
    }

    // מחנכת: התלמידות שלה (עם קודים). הורה: הילדות שלה (בלי קודים)
    [HttpGet]
    [ProducesResponseType(typeof(List<StudentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _students.GetMineAsync(User.GetUserId(), User.GetRole(), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _students.GetByIdAsync(id, User.GetUserId(), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Teacher))]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        var result = await _students.CreateAsync(request, User.GetUserId(), ct);
        if (!result.IsSuccess)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }
}
