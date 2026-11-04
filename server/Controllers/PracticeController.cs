using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Entities;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/practice")]
    [Authorize]
    public class PracticeController(IPracticeService practice)
        : ControllerBase
    {
        [HttpGet("quizzes")]
        public async Task<IActionResult> GetQuizzes(
            [FromQuery] int? category,
            [FromQuery] int? language,
            [FromQuery] QuizDifficulty? difficulty)
        {
            var result = await practice.GetQuizzesAsync(
                category, language, difficulty);

            return Ok(result);
        }

        [HttpGet("quizzes/{id}")]
        public async Task<IActionResult> GetQuiz(int id)
        {
            var result = await practice.GetQuizAsync(id);

            return Ok(result);
        }

        [HttpPost("answer")]
        public async Task<IActionResult> CheckAnswer(
            [FromBody] PracticeAnswerRequest request)
        {
            var result = await practice.CheckAnswerAsync(request);

            return Ok(result);
        }
    }
}