using Microsoft.Extensions.Options;
using NewsArticles.Models;
using NewsArticles.Models.Configurations;
using NewsArticles.Services.Interfaces;
using System.Text.Json;

namespace NewsArticles.Services.Implementations
{
    public class NewsStoriesClientService: INewsStoriesClientService
    {
        private readonly HttpClient _http;
        private readonly NewsStoriesCacheOptions _options;

        public NewsStoriesClientService(HttpClient http, IOptions<NewsStoriesCacheOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<List<int>> GetNewestStoryIdsAsync(CancellationToken ct = default)
        {
            var ids = await _http.GetFromJsonAsync<List<int>>("newstories.json", ct);
            return ids ?? new();
        }

        public async Task<NewsStoryDto?> GetStoryAsync(int id, CancellationToken ct = default)
        {
            var data = await _http.GetFromJsonAsync<JsonElement>($"item/{id}.json", ct);
            if (data.ValueKind == JsonValueKind.Undefined ||
                data.ValueKind == JsonValueKind.Null)
                return null;

            int storyId = data.GetProperty("id").GetInt32();
            string? title = data.TryGetProperty("title", out var t) ? t.GetString() : null;
            string? url = data.TryGetProperty("url", out var u) ? u.GetString() : null;
            string? by = data.TryGetProperty("by", out var b) ? b.GetString() : null;
            long? time = data.TryGetProperty("time", out var tm) ? tm.GetInt64() : null;
            int? score = data.TryGetProperty("score", out var sc) ? sc.GetInt32() : null;

            return new NewsStoryDto(storyId, title, url, by, time, score);
        }
    }
}
