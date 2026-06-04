namespace PvvConfig.Application.Exceptions;

/// <summary>Thrown when authentication fails (maps to HTTP 401).</summary>
public class UnauthorizedException(string message) : Exception(message);
