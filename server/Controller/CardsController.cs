using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Models;

namespace QuattroLingo.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/sets")]
    public class CardsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CardsController(AppDbContext db)
        {
            _db = db;
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
