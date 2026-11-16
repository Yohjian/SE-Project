namespace QuattroLingo.DTOs.Request
{
    public record PracticeAnswerRequest(
        int QuizId,
        int QuestionId,
        int AnswerOptionId
    );
}