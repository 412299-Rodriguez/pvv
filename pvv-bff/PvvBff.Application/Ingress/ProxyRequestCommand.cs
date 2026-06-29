using System.Text.Json;
using MediatR;

namespace PvvBff.Application.Ingress;

/// <summary>
/// The single ingress use case: resolve <see cref="Hash"/> to a route and either
/// proxy it to an internal service or run its in-process handler.
/// </summary>
public sealed record ProxyRequestCommand(
    string Hash,
    JsonElement? Body,
    string? CompanyId,
    string? SessionId,
    string CorrelationId) : IRequest<IngressResponse>;
