namespace QuattroLingo.Exceptions;

/// <summary>A requested resource does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);