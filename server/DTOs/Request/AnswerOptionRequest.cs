namespace QuattroLingo.DTOs.Request
{
    public record AnswerOptionRequest(
        string Text,
        bool IsCorrect
    );
}