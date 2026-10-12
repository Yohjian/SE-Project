using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record CreateSet
    (
        [Required] string Name
    );
}
