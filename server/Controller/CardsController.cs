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
        public async Task<IActionResult> AddCard(int setID, [FromBody] DTO.Request.CreateCard request)
        {
            var userID = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            var set = await _db.Sets.FirstOrDefaultAsync(s => s.ID == setID && s.UserID == userID);
            if (set == null) return NotFound();

            var card = new VocabularyCard
            {
                Term = request.Term,
                Definition = request.Definition,
                SetID = setID
            };

            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            return Ok(new DTO.Response.CardResponse(card.ID, card.Term, card.Definition));
        }

        [HttpGet("{setId}/cards")]
        public async Task<IActionResult> GetCards(int setID)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var set = await _db.Sets.FirstOrDefaultAsync(s => s.ID == setID && s.UserID == userId);
            if (set == null) return NotFound();

            var cards = await _db.Cards
                .Where(c => c.SetID == setID)
                .Select(c => new DTO.Response.CardResponse(c.ID, c.Term, c.Definition))
                .ToListAsync();

            return Ok(cards);
        }
    }
}
