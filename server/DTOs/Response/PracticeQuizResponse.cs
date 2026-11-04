namespace QuattroLingo.DTOs.Response
{
    public record PracticeQuizResponse(
        int Id,
        string Title,
        string? Description,
        List<PracticeQuestionResponse> Questions
    );

    public record PracticeQuestionResponse(
        int Id,
        string Text,
        int OrderIndex,
        List<PracticeAnswerOptionResponse> AnswerOptions
    );

    public record PracticeAnswerOptionResponse(
        int Id,
        string Text
    );
}