using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entity;

namespace QuattroLingo.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/sets")]
    public class SetsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SetsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSets()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var sets = await _db.Sets
                .Where(s => s.UserId == userId)
                .Select(s => new DTO.Response.SetSummary(s.Id, s.Name, s.Cards.Count, s.CreatedAt))
                .ToListAsync();

            return Ok(sets);
        }

        [HttpGet("{setId}")]
        public async Task<IActionResult> GetSet(int setId)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var set = await _db.Sets
            .Where(s => s.UserId == userId && s.Id == setId)
            .Select(s => new DTO.Response.SetContents(
                s.Id,
                s.Name,
                s.Cards.Select(c => new DTO.Response.CardResponse(c.Id, c.Term, c.Definition)).ToList()
                )
            )
            .FirstOrDefaultAsync();

            if (set == null)
                return NotFound("Set not found.");

            return Ok(set);
        }

        [HttpPost]
        public async Task<IActionResult> AddSet([FromBody] DTO.Request.CreateSet request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId == null) return Unauthorized();

            var set = new VocabularySet
            {
                UserId = userId,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };

            _db.Sets.Add(set);
            await _db.SaveChangesAsync();

            return Ok(new DTO.Response.SetSummary(set.Id, set.Name, 0, set.CreatedAt));
        }

        [HttpPost("{setId}/cards")]
        public async Task<IActionResult> AddCard(int setId, [FromBody] DTO.Request.CreateCard request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            var set = await _db.Sets.FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId);
            if (set == null) return NotFound();

            var card = new VocabularyCard
            {
                Term = request.Term,
                Definition = request.Definition,
                SetId = setId
            };

            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            return Ok(new DTO.Response.CardResponse(card.Id, card.Term, card.Definition));
        }

        [HttpGet("{setId}/cards")]
        public async Task<IActionResult> GetCards(int setId)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var set = await _db.Sets.FirstOrDefaultAsync(s => s.Id == setId && s.UserId == userId);
            if (set == null) return NotFound();

            var cards = await _db.Cards
                .Where(c => c.SetId == setId)
                .Select(c => new DTO.Response.CardResponse(c.Id, c.Term, c.Definition))
                .ToListAsync();

            return Ok(cards);
        }

    }
}
