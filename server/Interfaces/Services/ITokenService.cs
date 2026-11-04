using QuattroLingo.Entities;

namespace QuattroLingo.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateAuthToken(ApplicationUser user);
    }
}
