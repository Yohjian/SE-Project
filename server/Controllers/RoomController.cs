using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.DTOs;
using QuattroLingo.Services;
using System.IdentityModel.Tokens.Jwt;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomController(IRoomService roomService) : ControllerBase
    {
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateRoom(CreateRoomDto dto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
            var result = await roomService.CreateRoomAsync(dto, userId);

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
        public async Task<IActionResult> JoinRoom(JoinRoomDto dto)
        {
            var result = await roomService.JoinRoomAsync(dto);

            return Ok(result);
        }
    }
}