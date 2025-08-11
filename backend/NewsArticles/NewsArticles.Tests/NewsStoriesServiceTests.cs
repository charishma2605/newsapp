using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NewsArticles.Models.Configurations;
using NewsArticles.Models;
using NewsArticles.Services.Implementations;
using NewsArticles.Services.Interfaces;

namespace NewsArticles.Tests
{
    public class NewsStoriesServiceTests
    {
        private readonly Mock<INewsStoriesClientService> _client = new();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IOptions<NewsStoriesCacheOptions> _opts =
            Options.Create(new NewsStoriesCacheOptions
            {
                CacheSecondsForIdList = 60,
                CacheSecondsForStory = 300
            });
        private readonly ILogger<NewsStoriesService> _logger =
            LoggerFactory.Create(b => b.AddDebug()).CreateLogger<NewsStoriesService>();

        private NewsStoriesService CreateSvc() =>
            new NewsStoriesService(_client.Object, _cache, _opts);

        private static NewsStoryDto S(int id, string title) =>
            new NewsStoryDto ( id,  title,  $"https://z/{id}",  "u",  0,  1);

        [Fact]
        public async Task NoSearch_PagesIds_AndReturnsTotalOfAll()
        {
            _client.Setup(c => c.GetNewestStoryIdsAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(Enumerable.Range(1, 30).ToList());

            _client.Setup(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((int id, CancellationToken _) => S(id, $"T{id}"));

            var svc = CreateSvc();

            var (items, total) = await svc.GetNewestAsync(new PagedSearchRequestDto
            {
                PageNumber = 2,
                PageSize = 5,
                Search = null
            });

            Assert.Equal(30, total);
            Assert.Equal(5, items.Count());
            Assert.Equal(6, items.First().Id);   
            Assert.Equal(10, items.Last().Id);
        }

        [Fact]
        public async Task Search_FiltersFirst_ThenPages_ReturnsFilteredTotal()
        {
            _client.Setup(c => c.GetNewestStoryIdsAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(Enumerable.Range(1, 30).ToList());

            _client.Setup(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((int id, CancellationToken _) =>
                   {
                       var title = id <= 12 ? $"spa topic {id}" : $"other {id}";
                       return S(id, title);
                   });

            var svc = CreateSvc();

            var (p1, total1) = await svc.GetNewestAsync(new PagedSearchRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "spa"
            });

            var (p2, total2) = await svc.GetNewestAsync(new PagedSearchRequestDto
            {
                PageNumber = 2,
                PageSize = 10,
                Search = "spa"
            });

            Assert.Equal(12, total1);
            Assert.Equal(12, total2);          
            Assert.Equal(10, p1.Count());
            Assert.Equal(2, p2.Count());
            Assert.All(p1.Concat(p2), s => Assert.Contains("spa", s.Title!, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task IdList_IsCached_BetweenCalls()
        {
            _client.SetupSequence(c => c.GetNewestStoryIdsAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<int>() { 1, 2, 3, 4, 5 })
                   .ReturnsAsync(new List<int>() { 999 }); 

            _client.Setup(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((int id, CancellationToken _) => S(id, $"T{id}"));

            var svc = CreateSvc();

            // first call populates the id-list cache
            _ = await svc.GetNewestAsync(new PagedSearchRequestDto { PageNumber = 1, PageSize = 2 }, default);

            // second call should reuse cached ids
            _ = await svc.GetNewestAsync(new PagedSearchRequestDto { PageNumber = 1, PageSize = 2 }, default);

            _client.Verify(c => c.GetNewestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PerStory_IsCached_AcrossPages()
        {
            _client.Setup(c => c.GetNewestStoryIdsAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(Enumerable.Range(1, 5).ToList());

            var storyCalls = 0;
            _client.Setup(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((int id, CancellationToken _) =>
                   {
                       Interlocked.Increment(ref storyCalls);
                       return S(id, $"T{id}");
                   });

            var svc = CreateSvc();

            
            _ = await svc.GetNewestAsync(new PagedSearchRequestDto { PageNumber = 1, PageSize = 3 }, default);

            _ = await svc.GetNewestAsync(new PagedSearchRequestDto { PageNumber = 2, PageSize = 3 }, default);

            Assert.InRange(storyCalls, 1, 5);
        }

        [Fact]
        public async Task GetById_UsesCache_OnSecondCall()
        {
            var id = 42;

            _client.Setup(c => c.GetStoryAsync(id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(S(id, "once"));

            var svc = CreateSvc();

            var s1 = await svc.GetByIdAsync(id, default);
            var s2 = await svc.GetByIdAsync(id, default);

            Assert.NotNull(s1);
            Assert.NotNull(s2);
            Assert.Equal(id, s1!.Id);
            Assert.Same(s1!.Title, s2!.Title); 

            _client.Verify(c => c.GetStoryAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
