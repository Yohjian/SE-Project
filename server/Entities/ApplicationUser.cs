using Microsoft.AspNetCore.Identity;

namespace QuattroLingo.Entity
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}