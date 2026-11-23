namespace QuattroLingo.DTOs.Response
{
    public record CardResponse(int Id, string Term, string Definition, Dictionary<string, object?> Metadata);
}
