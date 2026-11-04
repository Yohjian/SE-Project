namespace QuattroLingo.DTOs.Response
{
    public record PracticeAnswerResponse(
        bool IsCorrect,
        int CorrectAnswerOptionId
    );
}