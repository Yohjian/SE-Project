using Microsoft.AspNetCore.Identity;
using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> FindByEmailAsync(string email);
        Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    }
}