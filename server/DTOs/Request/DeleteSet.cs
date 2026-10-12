
using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record DeleteSet
    (
        [Required] int Id
    );
}
