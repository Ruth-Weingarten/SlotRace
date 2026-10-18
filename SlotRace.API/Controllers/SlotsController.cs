using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotRace.API.Extensions;
using SlotRace.API.Models;
using SlotRace.Application.Common;
using SlotRace.Application.DTOs.Slots;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Enums;

namespace SlotRace.API.Controllers;

// Controller "רזה": לקבל → לקרוא לשירות → לתרגם. בלי לוגיקה עסקית ובלי DbContext.
// [Authorize] בודק "מה את" (role). השירות בודק "של מי זה" (בעלות)
[ApiController]
[Route("api/slots")]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly ISlotService _slots;

    public SlotsController(ISlotService slots)
    {
        _slots = slots;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaged([FromQuery] SlotQuery query, CancellationToken ct)
    {
        var result = await _slots.GetPagedAsync(query, User.GetUserId(), User.GetRole(), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _slots.GetByIdAsync(id, User.GetUserId(), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Teacher))]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSlotRequest request, CancellationToken ct)
    {
        var result = await _slots.CreateAsync(request, User.GetUserId(), ct);
        if (!result.IsSuccess)
            return result.ToActionResult();

        // 201 + כותרת Location עם הכתובת של הסלוט החדש
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(Role.Teacher))]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateSlotRequest request, CancellationToken ct)
    {
        var result = await _slots.UpdateAsync(id, request, User.GetUserId(), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(Role.Teacher))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var result = await _slots.DeleteAsync(id, User.GetUserId(), ct);
        return result.ToActionResult();   // הצלחה → 204
    }

    [HttpPost("{id:int}/booking")]
    [Authorize(Roles = nameof(Role.Parent))]
    [ProducesResponseType(typeof(SlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book([FromRoute] int id, [FromBody] BookSlotRequest request, CancellationToken ct)
    {
        var result = await _slots.BookAsync(id, request, User.GetUserId(), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}/booking")]
    [Authorize(Roles = nameof(Role.Parent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelBooking([FromRoute] int id, CancellationToken ct)
    {
        var result = await _slots.CancelBookingAsync(id, User.GetUserId(), ct);
        return result.ToActionResult();
    }
}
