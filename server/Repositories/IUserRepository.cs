using Microsoft.AspNetCore.Identity;
using QuattroLingo.Entity;

namespace QuattroLingo.Repository
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> FindByEmailAsync(string email);
        Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    }
}