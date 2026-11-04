using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Interfaces.Services;

namespace QuattroLingo.Services
{
    public class PracticeService(AppDbContext db) : IPracticeService
    {
        public async Task<List<PracticeQuizSummary>> GetQuizzesAsync(
            int? category,
            int? language,
            QuizDifficulty? difficulty)
        {
            var query = db.Quizzes
                .AsNoTracking()
                .Where(q => q.Kind == QuizKind.Practice);

            if (category.HasValue)
                query = query.Where(q => q.CategoryId == category.Value);

            if (language.HasValue)
                query = query.Where(q => q.LanguageId == language.Value);

            if (difficulty.HasValue)
                query = query.Where(q => q.Difficulty == difficulty.Value);

            return await query
                .OrderBy(q => q.Title)
                .Select(q => new PracticeQuizSummary(
                    q.Id,
                    q.Title,
                    q.Description,
                    q.CategoryId,
                    q.LanguageId,
                    q.Difficulty,
                    q.Questions.Count))
                .ToListAsync();
        }

        public async Task<PracticeQuizResponse> GetQuizAsync(int id)
        {
            var quiz = await db.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q =>
                    q.Id == id && q.Kind == QuizKind.Practice)
                ?? throw new NotFoundException("Practice quiz not found.");

            return new PracticeQuizResponse(
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new PracticeQuestionResponse(
                        q.Id,
                        q.Text,
                        q.OrderIndex,
                        q.AnswerOptions
                            .OrderBy(a => a.Id)
                            .Select(a => new PracticeAnswerOptionResponse(
                                a.Id,
                                a.Text))
                            .ToList()))
                    .ToList());
        }

        public async Task<PracticeAnswerResponse> CheckAnswerAsync(
            PracticeAnswerRequest request)
        {
            var question = await db.Questions
                .AsNoTracking()
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q =>
                    q.Id == request.QuestionId &&
                    q.QuizId == request.QuizId &&
                    q.Quiz != null &&
                    q.Quiz.Kind == QuizKind.Practice)
                ?? throw new NotFoundException("Practice question not found.");

            var selectedAnswer = question.AnswerOptions
                .FirstOrDefault(a => a.Id == request.AnswerOptionId)
                ?? throw new ValidationException(
                    "Answer option does not belong to this question.");

            var correctAnswer = question.AnswerOptions
                .SingleOrDefault(a => a.IsCorrect)
                ?? throw new ValidationException(
                    "Question must have exactly one correct answer.");

            return new PracticeAnswerResponse(
                selectedAnswer.IsCorrect,
                correctAnswer.Id);
        }
    }
}