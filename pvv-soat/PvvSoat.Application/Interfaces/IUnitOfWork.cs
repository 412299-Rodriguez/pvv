namespace PvvSoat.Application.Interfaces;

/// <summary>
/// Abstraction over the persistence transaction boundary so Application handlers
/// can commit changes without depending on EF Core directly.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a single serializable transaction.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}
