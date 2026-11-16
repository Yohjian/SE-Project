using QuattroLingo.Entities;
namespace QuattroLingo.DTOs.Request
{
    public record CreateQuizRequest(
        string Title,
        string? Description,
        List<QuestionRequest> Questions,
        QuizKind Kind  = QuizKind.Custom,
        int? LanguageId = null,
        int? CategoryId = null
    );
}
