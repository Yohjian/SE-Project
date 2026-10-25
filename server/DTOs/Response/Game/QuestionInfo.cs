namespace QuattroLingo.DTOs.Response.Game
{
    public record QuestionInfo(int Index, int Total, string Text, IReadOnlyList<OptionInfo> Options, int DurationMs);
}
