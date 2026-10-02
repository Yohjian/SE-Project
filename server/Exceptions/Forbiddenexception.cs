namespace QuattroLingo.Exceptions;

/// <summary>The caller is authenticated but not allowed to do this. Mapped to HTTP 403.</summary>
public class ForbiddenException(string message) : Exception(message);