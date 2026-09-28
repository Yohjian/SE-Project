using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTO.Request
{
    public record CreateCard
    (
        [Required] string Term,
        [Required] string Definition
    );

    public record UpdateCard
    (
        [Required] string Term,
        [Required] string Definition
    );

    public record DeleteCard
    (
        [Required] int Id
    );
}

namespace QuattroLingo.DTO.Response
{
    public record CardResponse(int Id, string Term, string Definition);
}
