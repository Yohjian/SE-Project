using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.DTOs.Request;

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
            Ok(await setService.CreateAsync(UserId, request.Name, request.FolderId!.Value));

        [HttpDelete("{setId}")]
        public async Task<IActionResult> DeleteSet(int setId)
        {
            await setService.DeleteSetAsync(UserId, setId);
            return NoContent();
        }

        [HttpDelete("{setId}/cards/{cardId}")]
        public async Task<IActionResult> DeleteCard(int setId, int cardId)
        {
            await setService.DeleteCardAsync(UserId, setId, cardId);
            return NoContent();
        }

        [HttpPost("{setId}/cards")]
        public async Task<IActionResult> AddCard(int setId, [FromBody] DTOs.Request.CreateCard request) =>
            Ok(await setService.AddCardAsync(UserId, setId, request.Term, request.Definition, request.Metadata));

        [HttpGet("{setId}/cards")]
        public async Task<IActionResult> GetCards(int setId, [FromQuery] string? sortBy, [FromQuery] bool sortAsc = true) =>
            Ok(await setService.GetCardsAsync(UserId, setId, sortBy, sortAsc));

        [HttpGet("{setId}/download/{fileType}")]
        public async Task<IActionResult> DownloadSet(int setId, FileType fileType)
        {
            DownloadFile download = await setService.DownloadSetAsync(UserId, setId, fileType);
            return File(download.Content, download.ContentType, download.FileName);
        }

        [HttpPatch("{setId}/metadata")]
        public async Task<IActionResult> SetSetMetadata(int setId, [FromBody] MetadataEntryRequest request)
        {
            await setService.SetSetMetadataAsync(UserId, setId, request.Key, request.Value);
            return NoContent();
        }

        [HttpPatch("{setId}/cards/{cardId}/metadata")]
        public async Task<IActionResult> SetCardMetadata(int setId, int cardId, [FromBody] MetadataEntryRequest request)
        {
            await setService.SetCardMetadataAsync(UserId, setId, cardId, request.Key, request.Value);
            return NoContent();
        }
    }
}
