namespace PvvBff.Application.Ingress;

/// <summary>
/// Uniform envelope returned to the frontend for every ingress call, regardless
/// of whether it was proxied or handled in-process.
/// </summary>
public sealed record IngressResponse(int StatusCode, object? Data, string? Error)
{
    public static IngressResponse Success(int statusCode, object? data) => new(statusCode, data, null);

    public static IngressResponse Failure(int statusCode, string error) => new(statusCode, null, error);
}
