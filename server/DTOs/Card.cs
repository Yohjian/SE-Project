using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
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

namespace QuattroLingo.DTOs.Response
{
    public record CardResponse(int Id, string Term, string Definition);
}
