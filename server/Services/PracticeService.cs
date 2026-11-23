using System.Text.Json;
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
            string userId,
            int? category,
            int? language,
            QuizDifficulty? difficulty)
        {
            var query = db.Quizzes
                .AsNoTracking()
                .Where(q =>
                q.Kind == QuizKind.Practice &&
                (q.OwnerId == userId ||
                (q.IsPublic && q.Owner!.UserName == "practice-seed")));

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

        public async Task<PracticeQuizResponse> GetQuizAsync(int id, string userId)
        {
            var quiz = await db.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q =>
                    q.Id == id && q.Kind == QuizKind.Practice &&
                    (q.OwnerId == userId ||
                    (q.IsPublic && q.Owner!.UserName == "practice-seed")))
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
            PracticeAnswerRequest request, string userId)
        {
            var question = await db.Questions
                .AsNoTracking()
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q =>
                    q.Id == request.QuestionId &&
                    q.QuizId == request.QuizId &&
                    q.Quiz != null &&
                    q.Quiz.Kind == QuizKind.Practice &&
                    (q.Quiz.OwnerId == userId ||
                    (q.Quiz.IsPublic &&
                    q.Quiz.Owner!.UserName == "practice-seed")))
                ?? throw new NotFoundException("Practice question not found.");

            var selectedAnswer = question.AnswerOptions
                .FirstOrDefault(a => a.Id == request.AnswerOptionId)
                ?? throw new ValidationException(
                    "Answer option does not belong to this question.");

            var correctAnswer = question.AnswerOptions
                .SingleOrDefault(a => a.IsCorrect)
                ?? throw new ValidationException(
                    "Question must have exactly one correct answer.");

            if (question.CardId.HasValue)
            {
                var card = await db.Cards.FindAsync(question.CardId.Value);
                if (card != null)
                {
                    if (selectedAnswer.IsCorrect)
                        card.Meta.Set("times_correct", card.Meta.GetOrDefault<int>("times_correct", 0) + 1);
                    else
                        card.Meta.Set("times_wrong", card.Meta.GetOrDefault<int>("times_wrong", 0) + 1);

                    card.Meta.Set("times_played", card.Meta.GetOrDefault<int>("times_played", 0) + 1);
                    card.Meta.Set("last_played", DateTime.UtcNow);
                    var json = JsonSerializer.Serialize(card.Meta.ToSerializable());
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE \"Cards\" SET \"Meta\" = {json}::jsonb WHERE \"Id\" = {card.Id}");
                }
            }

            return new PracticeAnswerResponse(
                selectedAnswer.IsCorrect,
                correctAnswer.Id);
        }

        public async Task<PracticeFiltersResponse> GetFiltersAsync()
        {
            var languages = await db.Languages
                .AsNoTracking()
                .OrderBy(l => l.Name)
                .Select(l => new PracticeFilterOptionResponse(l.Id, l.Name))
                .ToListAsync();

            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new PracticeFilterOptionResponse(c.Id, c.Name))
                .ToListAsync();

            return new PracticeFiltersResponse(languages, categories);
        }
    }
}
