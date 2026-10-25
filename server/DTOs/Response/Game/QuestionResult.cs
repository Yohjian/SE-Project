namespace QuattroLingo.DTOs.Response.Game
{
    public record QuestionResult(int QuestionIndex, int CorrectOptionId, IReadOnlyDictionary<int, int> OptionCounts);
}
