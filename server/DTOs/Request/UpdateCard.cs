using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record UpdateCard
(
    [Required] string Term,
    [Required] string Definition
);
}
