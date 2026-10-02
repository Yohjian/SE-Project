using QuattroLingo.DTO.Response;
using QuattroLingo.Entity;
using QuattroLingo.Exceptions;
using QuattroLingo.Repository;

namespace QuattroLingo.Service
{
    public class AuthService(IUserRepository users, ITokenService tokens) : IAuthService
    {
        public async Task RegisterAsync(string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Role = UserRole.User,
                IsActive = true
            };

            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

                throw new ValidationException("Registration failed.", errors);
            }
        }

        public async Task<AuthResponse> LoginAsync(string email, string password)
        {
            var user = await users.FindByEmailAsync(email);

            if (user is null || !await users.CheckPasswordAsync(user, password))
                throw new UnauthorizedException("Invalid email or password.");

            if (!user.IsActive)
                throw new ForbiddenException("This account has been deactivated.");

            return new AuthResponse(tokens.GenerateAuthToken(user), user.Email!, user.Role.ToString());
        }
    }
}