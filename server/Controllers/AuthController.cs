using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.Services;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DTOs.Request.Register request)
        {
            await authService.RegisterAsync(request.Email, request.Password);
            return Ok(new { message = "User registered successfully." });
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTOs.Request.Login request)
        {
            var response = await authService.LoginAsync(request.Email, request.Password);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me() => Ok(new
        {
            id = User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            email = User.FindFirstValue(JwtRegisteredClaimNames.Email),
            role = User.FindFirstValue(TokenService.RoleClaimType)
        });
    }
}
