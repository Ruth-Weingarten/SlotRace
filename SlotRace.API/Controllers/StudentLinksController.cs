using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotRace.API.Extensions;
using SlotRace.API.Models;
using SlotRace.Application.DTOs.Students;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Enums;

namespace SlotRace.API.Controllers;

// כתובת מפורשת: [controller] היה נותן "api/StudentLinks" ולא "api/student-links"
[ApiController]
[Route("api/student-links")]
[Authorize(Roles = nameof(Role.Parent))]
public class StudentLinksController : ControllerBase
{
    private readonly IStudentService _students;

    public StudentLinksController(IStudentService students)
    {
        _students = students;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Link([FromBody] LinkStudentRequest request, CancellationToken ct)
    {
        var result = await _students.LinkAsync(request, User.GetUserId(), ct);
        return result.ToActionResult();
    }
}
