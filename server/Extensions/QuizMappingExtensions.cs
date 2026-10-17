using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;

namespace QuattroLingo.Extensions
{
    public static class QuizMappingExtensions
    {
        public static QuizSummary ToSummary(this Quiz quiz) =>
            new(quiz.Id, quiz.Title, quiz.Description);

        public static AnswerOptionResponse ToAnswerResponse(this AnswerOption option) =>
            new(option.Id, option.Text, option.IsCorrect);
    }
}
