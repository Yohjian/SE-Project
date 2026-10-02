using QuattroLingo.DTO.Response;

namespace QuattroLingo.Service
{
    public interface IAuthService
    {
        /// <exception cref="Exceptions.ValidationException">Email taken or password rejected.</exception>
        Task RegisterAsync(string email, string password);

        /// <exception cref="Exceptions.UnauthorizedException">Wrong email or password.</exception>
        /// <exception cref="Exceptions.ForbiddenException">Account is deactivated.</exception>
        Task<AuthResponse> LoginAsync(string email, string password);
    }
}