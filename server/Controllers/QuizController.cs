using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Services;
using System.IdentityModel.Tokens.Jwt;

namespace QuattroLingo.Controllers
{
    [ApiController]
    [Route("api/quizzes")]
    [Authorize]
    public class QuizController(IQuizService quizzes) : ControllerBase
    {
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.GetMyQuizzesAsync(userId);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.GetByIdAsync(userId, id);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateQuizRequest request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.CreateAsync(userId, request);

            return CreatedAtAction(
                nameof(Get),
                new { id = result.Id },
                result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            UpdateQuizRequest request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.UpdateAsync(userId, id, request);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            await quizzes.DeleteAsync(userId, id);

            return NoContent();
        }
        [HttpPost("{quizId}/questions")]
        public async Task<IActionResult> AddQuestion(
            int quizId,
            QuestionRequest request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.AddQuestionAsync(
                userId,
                quizId,
                request);

            return Ok(result);
        }

        [HttpPut("questions/{questionId}")]
        public async Task<IActionResult> UpdateQuestion(
            int questionId,
            QuestionRequest request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.UpdateQuestionAsync(
                userId,
                questionId,
                request);

            return Ok(result);
        }

        [HttpDelete("questions/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            await quizzes.DeleteQuestionAsync(userId, questionId);

            return NoContent();
        }

        [HttpPost("questions/{questionId}/answers")]
        public async Task<IActionResult> AddAnswerOption(
            int questionId,
            AnswerOptionRequest request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.AddAnswerOptionAsync(
                userId,
                questionId,
                request);

            return Ok(result);
        }

        [HttpPut("answers/{answerOptionId}")]
        public async Task<IActionResult> UpdateAnswerOption(
            int answerOptionId,
            AnswerOptionRequest request)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            var result = await quizzes.UpdateAnswerOptionAsync(
                userId,
                answerOptionId,
                request);

            return Ok(result);
        }

        [HttpDelete("answers/{answerOptionId}")]
        public async Task<IActionResult> DeleteAnswerOption(int answerOptionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            await quizzes.DeleteAnswerOptionAsync(
                userId,
                answerOptionId);

            return NoContent();
        }
    }
}