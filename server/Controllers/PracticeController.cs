using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Entities;
using QuattroLingo.Interfaces.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using QuattroLingo.Exceptions;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/practice")]
    [Authorize]
    public class PracticeController(IPracticeService practice)
        : ControllerBase
    {
        private string UserId =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedException("User id is missing from the token.");

        [HttpGet("quizzes")]
        public async Task<IActionResult> GetQuizzes(
            [FromQuery] int? category,
            [FromQuery] int? language,
            [FromQuery] QuizDifficulty? difficulty)
        {
            var result = await practice.GetQuizzesAsync(
                UserId, category, language, difficulty);

            return Ok(result);
        }

        [HttpGet("quizzes/{id}")]
        public async Task<IActionResult> GetQuiz(int id)
        {
            var result = await practice.GetQuizAsync(id, UserId);

            return Ok(result);
        }

        [HttpPost("answer")]
        public async Task<IActionResult> CheckAnswer(
            [FromBody] PracticeAnswerRequest request)
        {
            var result = await practice.CheckAnswerAsync(request, UserId);

            return Ok(result);
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var result = await practice.GetFiltersAsync();

            return Ok(result);
        }
    }
}