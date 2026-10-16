namespace QuattroLingo.DTOs.Response
{
    public record QuizEditorResponse(
        int Id,
        string Title,
        string? Description,
        List<QuestionResponse> Questions
    );
}