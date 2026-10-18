using Microsoft.AspNetCore.Mvc;
using SlotRace.API.Models;
using SlotRace.Application.Common;

namespace SlotRace.API.Extensions;

// "המתורגמן": המקום היחיד שמכיר גם את ErrorType (של השירותים) וגם את HTTP.
// כל Controller כותב return result.ToActionResult() במקום שורות של if
public static class ServiceResultExtensions
{
    // פעולה שמחזירה ערך: הצלחה → 200 עם הערך
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToError(result);

    // פעולה בלי ערך (מחיקה, ביטול): הצלחה → 204
    public static IActionResult ToActionResult(this ServiceResult result) =>
        result.IsSuccess ? new NoContentResult() : ToError(result);

    // טבלת התרגום: ErrorType → סטטוס HTTP, עם גוף אחיד
    private static IActionResult ToError(ServiceResult result)
    {
        var status = result.Error switch
        {
            ErrorType.Validation   => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
            ErrorType.NotFound     => StatusCodes.Status404NotFound,
            ErrorType.Conflict     => StatusCodes.Status409Conflict,
            _                      => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ErrorResponse(status, result.Message ?? "שגיאה.")) { StatusCode = status };
    }
}
