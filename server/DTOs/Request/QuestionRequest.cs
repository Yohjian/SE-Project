namespace QuattroLingo.DTOs.Request
{
    public record QuestionRequest(
        string Text,
        List<AnswerOptionRequest> AnswerOptions
    );
}