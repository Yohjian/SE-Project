using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record DeleteCard
(
    [Required] int Id
);
}
