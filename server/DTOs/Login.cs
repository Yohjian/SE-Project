using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTOs.Request
{
    public record Login(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
}
