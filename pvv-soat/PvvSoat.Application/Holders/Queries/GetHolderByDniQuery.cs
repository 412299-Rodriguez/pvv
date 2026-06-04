using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;

namespace PvvSoat.Application.Holders.Queries;

public record GetHolderByDniQuery(string Dni) : IRequest<HolderDto?>;

public class GetHolderByDniHandler(IHolderRepository holders)
    : IRequestHandler<GetHolderByDniQuery, HolderDto?>
{
    public async Task<HolderDto?> Handle(GetHolderByDniQuery request, CancellationToken ct)
    {
        var holder = await holders.GetByDniAsync(request.Dni, ct);
        return holder is null ? null : HolderDto.FromEntity(holder);
    }
}
