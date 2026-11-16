namespace QuattroLingo.DTOs.Response
{
    public record PracticeFilterOptionResponse(
        int Id,
        string Name
    );

    public record PracticeFiltersResponse(
        List<PracticeFilterOptionResponse> Languages,
        List<PracticeFilterOptionResponse> Categories
    );
}