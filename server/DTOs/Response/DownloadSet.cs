namespace QuattroLingo.DTOs.Response;

public record DownloadSet(MemoryStream Content, string ContentType, string FileName);
