using System.Data;
using Microsoft.EntityFrameworkCore;
using PvvSoat.Application.Interfaces;

namespace PvvSoat.Infrastructure.Persistence;

public class SoatUnitOfWork(SoatDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await operation(ct);
            await transaction.CommitAsync(ct);
        });
    }
}
