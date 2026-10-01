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
            var result = await authService.RegisterAsync(request.Email, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTO.Request.Login request)
        {
            var response = await authService.LoginAsync(request.Email, request.Password);

            if (response is null)
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(response);
        }
    }
}