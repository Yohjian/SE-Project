namespace QuattroLingo.Exceptions
{
    /// <summary>The request conflicts with the current state of a resource. Mapped to HTTP 409.</summary>
    public class ConflictException(string message) : Exception(message);
}