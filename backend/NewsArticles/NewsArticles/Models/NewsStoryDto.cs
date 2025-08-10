namespace NewsArticles.Models
{
    public record NewsStoryDto(
        int Id,
        string? Title,
        string? Url,
        string? By,
        long? Time,
        int? Score
    );
}
