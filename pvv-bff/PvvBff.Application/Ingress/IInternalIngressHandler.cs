namespace PvvBff.Application.Ingress;

/// <summary>
/// In-process handler for an <c>internal://{Key}</c> ingress route. Each concern
/// (company-config, plate-search, …) is one implementation; the ingress resolves
/// the right one by <see cref="Key"/> from a DI-built registry — no central
/// switch, so adding a concern means adding a class, not editing a dispatcher.
/// </summary>
public interface IInternalIngressHandler
{
    /// <summary>The internal route key this handler serves (e.g. "company-config").</summary>
    string Key { get; }

    Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct);
}
