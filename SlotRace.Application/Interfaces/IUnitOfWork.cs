namespace SlotRace.Application.Interfaces;

// פותח טרנזקציה בלי ש-Application תכיר את EF.
// DbContext הוא Scoped, ולכן הטרנזקציה חלה על כל ה-repositories באותה בקשה
public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct);
}
