namespace QuattroLingo.DTOs.Response
{
    public record QuestionResponse(
        int Id,
        string Text,
        List<AnswerOptionResponse> AnswerOptions
    );
}