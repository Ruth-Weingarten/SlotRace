namespace SlotRace.Application.Interfaces;

// טרנזקציה פתוחה. IAsyncDisposable מאפשר "await using":
// כשהבלוק נגמר בלי CommitAsync, הטרנזקציה מתבטלת (Rollback). לכן אין צורך ב-RollbackAsync
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
