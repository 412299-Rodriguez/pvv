using PvvBff.Application.Ingress;

namespace PvvBff.Application.Abstractions;

/// <summary>Forwards a resolved ingress call to its internal service over HTTP.</summary>
public interface IInternalHttpProxy
{
    Task<IngressResponse> SendAsync(IngressExecutionContext context, CancellationToken ct);
}
