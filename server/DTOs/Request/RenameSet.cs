
using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record RenameSet
    (
        [Required] string NewName,
        [Required] int Id
    );
}
