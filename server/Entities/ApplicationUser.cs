using Microsoft.AspNetCore.Identity;

namespace QuattroLingo.Entity
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}