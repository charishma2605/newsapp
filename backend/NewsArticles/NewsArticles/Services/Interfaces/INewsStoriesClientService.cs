using NewsArticles.Models;

namespace NewsArticles.Services.Interfaces
{
    public interface INewsStoriesClientService
    {
        Task<List<int>> GetNewestStoryIdsAsync(CancellationToken ct = default);
        Task<NewsStoryDto?> GetStoryAsync(int id, CancellationToken ct = default);
    }
}
