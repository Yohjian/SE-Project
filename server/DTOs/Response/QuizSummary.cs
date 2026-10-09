namespace QuattroLingo.DTOs.Response
{
    public record QuizSummary(
        int Id,
        string Title,
        string? Description
    );
}