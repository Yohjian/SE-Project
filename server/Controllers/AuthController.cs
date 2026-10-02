using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.Service;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DTO.Request.Register request)
        {
            await authService.RegisterAsync(request.Email, request.Password);
            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTO.Request.Login request)
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