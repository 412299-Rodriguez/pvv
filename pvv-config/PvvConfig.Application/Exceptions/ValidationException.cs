namespace PvvConfig.Application.Exceptions;

/// <summary>Thrown when a business rule is violated (maps to HTTP 400).</summary>
public class ValidationException(string message) : Exception(message);
