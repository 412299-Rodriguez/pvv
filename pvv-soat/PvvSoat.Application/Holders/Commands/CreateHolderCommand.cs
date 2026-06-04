using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;

namespace PvvSoat.Application.Holders.Commands;

public record CreateHolderCommand(
    string DNI,
    string FirstName,
    string LastName,
    string Email,
    string Phone) : IRequest<HolderDto>;

public class CreateHolderHandler(IHolderRepository holders, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateHolderCommand, HolderDto>
{
    public async Task<HolderDto> Handle(CreateHolderCommand request, CancellationToken ct)
    {
        // Upsert by DNI: if it already exists, return the existing holder.
        var existing = await holders.GetByDniAsync(request.DNI, ct);
        if (existing is not null)
        {
            return HolderDto.FromEntity(existing);
        }

        var holder = new Holder
        {
            HolderId = Guid.NewGuid(),
            DNI = request.DNI,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        await holders.AddAsync(holder, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return HolderDto.FromEntity(holder);
    }
}
