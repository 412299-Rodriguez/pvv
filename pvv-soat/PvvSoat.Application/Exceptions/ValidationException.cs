namespace PvvSoat.Application.Exceptions;

/// <summary>Thrown when a business rule is violated, e.g. an expired budget
/// or a duplicate policy (maps to HTTP 400).</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
