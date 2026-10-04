using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record Register(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);
}
