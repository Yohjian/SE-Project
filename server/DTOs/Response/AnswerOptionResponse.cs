namespace QuattroLingo.DTOs.Response
{
    public record AnswerOptionResponse(
        int Id,
        string Text,
        bool IsCorrect
    );
}