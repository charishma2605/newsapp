using NewsArticles.Models;

namespace NewsArticles.Services.Interfaces
{
    public interface INewsStoriesService
    {
        Task<(IEnumerable<NewsStoryDto>,int)> GetNewestAsync(PagedSearchRequestDto requestDto, CancellationToken ct = default);
        Task<NewsStoryDto?> GetByIdAsync(int id, CancellationToken ct = default);
    }
}
