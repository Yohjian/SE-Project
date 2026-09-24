using Microsoft.AspNetCore.Mvc;

// This controller will be deleted after, it's only used for testing now

namespace QuattroLingo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok(new { message = "Backend loaded successfully." });
    }
}