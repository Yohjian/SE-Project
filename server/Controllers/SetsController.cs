using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.Exceptions;
using QuattroLingo.Services;

namespace QuattroLingo.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/sets")]
    public class SetsController(ISetService setService) : ControllerBase
    {
        private string UserId =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedException("User id is missing from the token.");

        [HttpGet]
        public async Task<IActionResult> GetAllSets() =>
            Ok(await setService.GetAllAsync(UserId));

        [HttpGet("{setId}")]
        public async Task<IActionResult> GetSet(int setId) =>
            Ok(await setService.GetAsync(UserId, setId));

        [HttpPost]
        public async Task<IActionResult> AddSet([FromBody] DTOs.Request.CreateSet request) =>
            Ok(await setService.CreateAsync(UserId, request.Name));

        [HttpPost("{setId}/cards")]
        public async Task<IActionResult> AddCard(int setId, [FromBody] DTOs.Request.CreateCard request) =>
            Ok(await setService.AddCardAsync(UserId, setId, request.Term, request.Definition));

        [HttpGet("{setId}/cards")]
        public async Task<IActionResult> GetCards(int setId) =>
            Ok(await setService.GetCardsAsync(UserId, setId));
    }
}