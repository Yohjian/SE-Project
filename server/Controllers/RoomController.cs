using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomController(IRoomService roomService) : ControllerBase
    {
        private string UserId =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedException("User id is missing from the token.");

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateRoom(CreateRoom dto)
        {
            var result = await roomService.CreateRoomAsync(dto, UserId);

            return CreatedAtAction(
                nameof(GetByCode),
                new { code = result.JoinCode },
                result);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicRooms()
        {
            var result = await roomService.GetPublicRoomAsync();

            return Ok(result);
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyRooms()
        {
            var result = await roomService.GetMyRoomsAsync(UserId);

            return Ok(result);
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var result = await roomService.GetByCodeAsync(code);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom(JoinRoom dto)
        {
            var result = await roomService.JoinRoomAsync(dto);

            return Ok(result);
        }
    }
}