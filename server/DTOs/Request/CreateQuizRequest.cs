namespace QuattroLingo.DTOs.Request
{
    public record CreateQuizRequest(
        string Title,
        string? Description,
        List<QuestionRequest> Questions
    );
}