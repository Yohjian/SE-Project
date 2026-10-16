using QuattroLingo.Entities;

namespace QuattroLingo.Services
{
    public interface ITokenService
    {
        string GenerateAuthToken(ApplicationUser user);
    }
}