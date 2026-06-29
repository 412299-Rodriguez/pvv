using System.Text.Json;

namespace PvvBff.API.Models;

/// <summary>
/// The frontend's ingress envelope: an opaque hash plus an optional JSON body.
/// The company token and session id travel as headers, not in this payload.
/// </summary>
public sealed record IngressHttpRequest(string Hash, JsonElement? Body);
