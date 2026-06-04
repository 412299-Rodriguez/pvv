using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;

namespace PvvSoat.Application.Policies.Queries;

public record GetPolicyByNumberQuery(string PolicyNumber) : IRequest<PolicyDto?>;

public class GetPolicyByNumberHandler(IPolicyRepository policies)
    : IRequestHandler<GetPolicyByNumberQuery, PolicyDto?>
{
    public async Task<PolicyDto?> Handle(GetPolicyByNumberQuery request, CancellationToken ct)
    {
        var policy = await policies.GetByPolicyNumberAsync(request.PolicyNumber, ct);
        return policy is null ? null : PolicyDto.FromEntity(policy);
    }
}
