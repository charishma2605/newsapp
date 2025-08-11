using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NewsArticles.Constants;
using NewsArticles.Models;
using NewsArticles.Models.Configurations;
using NewsArticles.Services.Interfaces;
using NewsArticles.Utilities;

namespace NewsArticles.Services.Implementations
{
    public class NewsStoriesService: INewsStoriesService
    {
        private readonly INewsStoriesClientService _client;
        private readonly IMemoryCache _cache;
        private readonly NewsStoriesCacheOptions _options;
        

        public NewsStoriesService(INewsStoriesClientService client, IMemoryCache cache, IOptions<NewsStoriesCacheOptions> options)
        {
            _client = client;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<(IEnumerable<NewsStoryDto>, int)> GetNewestAsync(PagedSearchRequestDto requestDto, CancellationToken ct = default)
        {
            requestDto = requestDto.Normalize();

            requestDto.Search = string.IsNullOrWhiteSpace(requestDto.Search) ? null : requestDto.Search!.Trim();

            // Cache the ID list briefly
            var ids = await _cache.GetOrCreateAsync(CacheKeys.IdCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheSecondsForIdList);
                return await _client.GetNewestStoryIdsAsync(ct);
            }) ?? new();


            if (string.IsNullOrEmpty(requestDto.Search))
            {
                var pageIds = ids.Page(requestDto.PageNumber, requestDto.PageSize).ToList();

                var items = (await Task.WhenAll(pageIds.Select(x=>GetStoryCached(x, ct))))
                    .Where(s => s is not null)!.Cast<NewsStoryDto>().ToList();
                return (items, ids.Count);
            }

            var result=await GetSearchedStories(requestDto, ids, ct);

            return result;
        }

        public Task<NewsStoryDto?> GetByIdAsync(int id, CancellationToken ct = default) => GetStoryCached(id, ct);

        private Task<NewsStoryDto?> GetStoryCached(int id, CancellationToken ct)
        {
            string cacheKey = $"{CacheKeys.StoryCacheKey}{id}";
            return _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheSecondsForStory);
                return await _client.GetStoryAsync(id, ct);
            });
        }

        private async Task<(IEnumerable<NewsStoryDto>, int)> GetSearchedStories(PagedSearchRequestDto requestDto,List<int> ids, CancellationToken ct)
        {
            var searchKey = $"{CacheKeys.SearchCacheKey}{requestDto.Search?.ToLowerInvariant()}";
            var matchedIds = await _cache.GetOrCreateAsync(searchKey, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                var all = await Task.WhenAll(ids.Select(x => GetStoryCached(x, ct)));
                return all.Where(s => s is not null &&
                                      (s!.Title ?? "").Contains(requestDto.Search, StringComparison.OrdinalIgnoreCase))
                          .Select(s => s!.Id)
                          .ToList();
            }) ?? new List<int>();

            var total = matchedIds.Count;
            var pageIds = matchedIds.Page(requestDto.PageNumber, requestDto.PageSize).ToList();
            var items = (await Task.WhenAll(pageIds.Select(x => GetStoryCached(x, ct))))
                .Where(s => s is not null)!.Cast<NewsStoryDto>().ToList();

            return (items, total);
        }
    }
}
