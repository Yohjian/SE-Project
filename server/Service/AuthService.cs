using Microsoft.AspNetCore.Identity;
using QuattroLingo.DTO.Response;
using QuattroLingo.Entity;

namespace QuattroLingo.Service
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration) : IAuthService
    {
        public async Task<IdentityResult> RegisterAsync(string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            return await userManager.CreateAsync(user, password);
        }

        public async Task<AuthResponse?> LoginAsync(string email, string password)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null || !await userManager.CheckPasswordAsync(user, password))
                return null;

            var token = Jwt.GenerateAuthToken(configuration, user);
            return new AuthResponse(token, user.Email!);
        }
    }
}