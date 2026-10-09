namespace QuattroLingo.DTOs.Request
{
    public record QuestionRequest(
        string Text,
        int TimeLimitSeconds,
        int Points,
        int OrderIndex,
        List<AnswerOptionRequest> AnswerOptions
    );
}