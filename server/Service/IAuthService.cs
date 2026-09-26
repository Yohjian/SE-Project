using Microsoft.AspNetCore.Identity;
using QuattroLingo.DTO.Response;

namespace QuattroLingo.Service
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(string email, string password);
        Task<AuthResponse?> LoginAsync(string email, string password);
    }
}