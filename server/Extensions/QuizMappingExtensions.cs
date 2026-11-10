using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;

namespace QuattroLingo.Extensions
{
    public static class QuizMappingExtensions
    {
        public static QuizSummary ToSummary(this Quiz quiz) =>
            new(quiz.Id, quiz.Title, quiz.Description, quiz.IsPublic, !string.IsNullOrWhiteSpace(quiz.Owner?.DisplayName) ? quiz.Owner.DisplayName : quiz.Owner?.Email?.Split('@')[0] ?? "Unknown");

        public static AnswerOptionResponse ToAnswerResponse(this AnswerOption option) =>
            new(option.Id, option.Text, option.IsCorrect);
    }
}
