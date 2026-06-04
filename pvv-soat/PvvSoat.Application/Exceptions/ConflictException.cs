namespace PvvSoat.Application.Exceptions;

/// <summary>Thrown when an operation conflicts with current state (maps to HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
