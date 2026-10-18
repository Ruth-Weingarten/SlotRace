using SlotRace.Application.Interfaces;

namespace SlotRace.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;   // אותו מופע כמו של ה-repositories (Scoped)

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct)
    {
        // פותחים טרנזקציה של EF, ו"עוטפים" אותה בממשק שלנו
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransaction(transaction);
    }
}
