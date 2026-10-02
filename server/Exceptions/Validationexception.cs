namespace QuattroLingo.Exceptions;

/// <summary>
/// A business rule or input check failed. Mapped to HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>(errors ?? new Dictionary<string, string[]>());
    }
}