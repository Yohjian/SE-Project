namespace QuattroLingo.Exceptions;

/// <summary>The caller could not be authenticated (e.g. bad credentials). Mapped to HTTP 401.</summary>
public class UnauthorizedException(string message) : Exception(message);