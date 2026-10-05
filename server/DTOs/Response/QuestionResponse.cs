namespace QuattroLingo.DTOs.Response
{
    public record QuestionResponse(
        int Id,
        string Text,
        int TimeLimitSeconds,
        int Points,
        int OrderIndex,
        List<AnswerOptionResponse> AnswerOptions
    );
}