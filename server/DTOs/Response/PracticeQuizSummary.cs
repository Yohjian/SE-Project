using QuattroLingo.Entities;

namespace QuattroLingo.DTOs.Response
{
    public record PracticeQuizSummary(
        int Id,
        string Title,
        string? Description,
        int? CategoryId,
        int? LanguageId,
        QuizDifficulty? Difficulty,
        int QuestionCount
    );
}