using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTO.Request
{
    public record CreateSet
    {
        [Required]
        public string Name { get; init; } = string.Empty;
    }


}

namespace QuattroLingo.DTO.Response
{
    public record SetContents(
        int Id,
        string Name,
        ICollection<CardResponse> Cards
    );

    public record SetSummary(
        int Id,
        string Name,
        int CardCount,
        DateTime CreatedAt
    );

    public record SetList(
        [Required] string Name
    );
}
