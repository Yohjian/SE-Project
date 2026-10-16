namespace QuattroLingo.DTOs.Response
{
    public record SetContents
    (
        int Id,
        string Name,
        ICollection<CardResponse> Cards
    );
}
