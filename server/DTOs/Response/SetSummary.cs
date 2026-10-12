namespace QuattroLingo.DTOs.Response
{
    public record SetSummary
    (
        int Id,
        string Name,
        int CardCount,
        DateTime CreatedAt
    );
}
