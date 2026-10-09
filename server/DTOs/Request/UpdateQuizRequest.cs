namespace QuattroLingo.DTOs.Request
{
    public record UpdateQuizRequest(
        string Title,
        string? Description,
        List<QuestionRequest> Questions
    );
}