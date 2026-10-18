using Microsoft.EntityFrameworkCore.Storage;
using SlotRace.Application.Interfaces;

namespace SlotRace.Infrastructure.Persistence;

// Adapter: מתאים את הטרנזקציה של EF (IDbContextTransaction) לממשק שלנו
public class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken ct) =>
        _transaction.CommitAsync(ct);

    // נקרא אוטומטית בסוף "await using". אם לא היה Commit, EF מבטל את הטרנזקציה
    public ValueTask DisposeAsync() =>
        _transaction.DisposeAsync();
}
