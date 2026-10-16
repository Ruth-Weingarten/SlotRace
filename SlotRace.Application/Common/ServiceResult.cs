namespace SlotRace.Application.Common;

// התוצאה של פעולה שלא מחזירה ערך (למשל מחיקה).
public class ServiceResult
{
    public ErrorType Error { get; }
    public string? Message { get; }
    public bool IsSuccess => Error == ErrorType.None;

    // protected: אפשר ליצור תוצאה רק דרך Ok ו-Fail, או מתוך המחלקה שיורשת.
    protected ServiceResult(ErrorType error, string? message)
    {
        Error = error;
        Message = message;
    }

    public static ServiceResult Ok() => new(ErrorType.None, null);

    public static ServiceResult Fail(ErrorType error, string message)
    {
        if (error == ErrorType.None)
            throw new ArgumentException("כישלון חייב סוג שגיאה.", nameof(error));

        return new(error, message);
    }
}

// התוצאה של פעולה שמחזירה ערך (למשל SlotDto).
public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; }

    private ServiceResult(T? value, ErrorType error, string? message)
        : base(error, message)
    {
        Value = value;
    }

    public static ServiceResult<T> Ok(T value) => new(value, ErrorType.None, null);

    // new: מסתירה את Fail של מחלקת הבסיס, כדי להחזיר ServiceResult<T> ולא ServiceResult.
    public static new ServiceResult<T> Fail(ErrorType error, string message)
    {
        if (error == ErrorType.None)
            throw new ArgumentException("כישלון חייב סוג שגיאה.", nameof(error));

        return new(default, error, message);
    }
}
