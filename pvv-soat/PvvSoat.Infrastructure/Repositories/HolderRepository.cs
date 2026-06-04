using Microsoft.EntityFrameworkCore;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Infrastructure.Persistence;

namespace PvvSoat.Infrastructure.Repositories;

public class HolderRepository(SoatDbContext context) : IHolderRepository
{
    public Task<Holder?> GetByDniAsync(string dni, CancellationToken ct) =>
        context.Holders.FirstOrDefaultAsync(h => h.DNI == dni, ct);

    public async Task AddAsync(Holder holder, CancellationToken ct) =>
        await context.Holders.AddAsync(holder, ct);

    public Task<bool> ExistsByIdAsync(Guid holderId, CancellationToken ct) =>
        context.Holders.AnyAsync(h => h.HolderId == holderId, ct);
}
