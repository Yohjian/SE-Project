using QuattroLingo.Entity;

namespace QuattroLingo.Service
{
    public interface ITokenService
    {
        string GenerateAuthToken(ApplicationUser user);
    }
}