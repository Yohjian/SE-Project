using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTO.Request
{
    public record Register(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);
}
