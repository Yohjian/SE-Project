using Microsoft.AspNetCore.Identity;
using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public class UserRepository(UserManager<ApplicationUser> userManager) : IUserRepository
    {
        public Task<ApplicationUser?> FindByEmailAsync(string email) =>
            userManager.FindByEmailAsync(email);

        public Task<IdentityResult> CreateAsync(ApplicationUser user, string password) =>
            userManager.CreateAsync(user, password);

        public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) =>
            userManager.CheckPasswordAsync(user, password);
    }
}