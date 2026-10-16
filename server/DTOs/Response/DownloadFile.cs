namespace QuattroLingo.DTOs.Response;

public record DownloadFile(MemoryStream Content, string ContentType, string FileName);
