using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.Service;
using QuattroLingo.DTO;

namespace QuattroLingo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<IdentityUser> userManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Requests.Register request)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Requests.Login request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = Jwt.GenerateAuthToken(configuration, user);
        return Ok(new { token, email = user.Email });
    }
}
