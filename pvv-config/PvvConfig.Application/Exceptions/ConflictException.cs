namespace PvvConfig.Application.Exceptions;

/// <summary>Thrown when an operation conflicts with current state (maps to HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);
