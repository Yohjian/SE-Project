
using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record CreateCard
    (
        [Required] string Term,
        [Required] string Definition
    );
}
