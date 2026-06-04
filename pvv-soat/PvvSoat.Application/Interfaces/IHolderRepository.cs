using PvvSoat.Domain.Entities;

namespace PvvSoat.Application.Interfaces;

public interface IHolderRepository
{
    Task<Holder?> GetByDniAsync(string dni, CancellationToken ct);
    Task AddAsync(Holder holder, CancellationToken ct);
    Task<bool> ExistsByIdAsync(Guid holderId, CancellationToken ct);
}
