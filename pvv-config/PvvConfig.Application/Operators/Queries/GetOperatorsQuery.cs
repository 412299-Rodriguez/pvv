using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Operators.Queries;

public record GetOperatorsQuery : IRequest<List<OperatorDto>>;

public class GetOperatorsHandler(IOperatorRepository operators)
    : IRequestHandler<GetOperatorsQuery, List<OperatorDto>>
{
    public async Task<List<OperatorDto>> Handle(GetOperatorsQuery request, CancellationToken ct)
    {
        var all = await operators.GetAllAsync(ct);
        return all.Select(OperatorDto.FromEntity).ToList();
    }
}
