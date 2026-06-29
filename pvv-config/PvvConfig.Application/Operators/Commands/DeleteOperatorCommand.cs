using MediatR;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Operators.Commands;

public record DeleteOperatorCommand(Guid OperatorId) : IRequest<bool>;

public class DeleteOperatorHandler(
    IOperatorRepository operators,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteOperatorCommand, bool>
{
    public async Task<bool> Handle(DeleteOperatorCommand request, CancellationToken ct)
    {
        var deleted = await operators.DeleteAsync(request.OperatorId, ct);
        if (deleted)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        return deleted;
    }
}
