using System.ComponentModel.DataAnnotations;

namespace QuattroLingo.DTO.Request
{
    public record Login(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
}
