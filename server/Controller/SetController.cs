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
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sets = await _db.Sets
                .Where(s => s.UserID == userID)
                .Select(s => new DTO.Response.SetSummary(s.ID, s.Name, s.Cards.Count, s.CreatedAt))
                .ToListAsync();

            return Ok(sets);
        }

        [HttpGet("{setID}")]
        public async Task<IActionResult> GetSet(int setID)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var set = await _db.Sets
            .Where(s => s.UserID == userID && s.ID == setID)
            .Select(s => new DTO.Response.SetContents(
                s.ID,
                s.Name,
                s.Cards.Select(c => new DTO.Response.CardResponse(c.ID, c.Term, c.Definition)).ToList()
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var set = new VocabularySet
            {
                UserID = userId,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow,
                Cards = []
            };

            _db.Sets.Add(set);
            await _db.SaveChangesAsync();

            return Ok(new DTO.Response.SetSummary(set.ID, set.Name, 0, set.CreatedAt));
        }
    }
}
