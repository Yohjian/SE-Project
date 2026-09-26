using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTO.Request
{
    public record CreateSet
    (
        [Required] string Name
    );

    public record RenameSet
    (
        [Required] string NewName,
        [Required] int Id
    );

    public record DeleteSet
    (
        [Required] int Id
    );

}

namespace QuattroLingo.DTO.Response
{
    public record SetContents
    (
        int Id,
        string Name,
        ICollection<CardResponse> Cards
    );

    public record SetSummary
    (
        int Id,
        string Name,
        int CardCount,
        DateTime CreatedAt
    );
}
