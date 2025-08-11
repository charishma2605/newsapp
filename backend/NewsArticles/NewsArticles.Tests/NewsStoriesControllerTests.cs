using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.Common;
using Moq;
using NewsArticles.Controllers;
using NewsArticles.Models;
using NewsArticles.Services.Interfaces;
using System.Linq;
using System.Text.Json;
using Xunit.Abstractions;

namespace NewsArticles.Tests
{
    public class NewsStoriesControllerTests
    {

        [Fact]
        public async Task Newest_ReturnsPayload()
        {
            var service = new Mock<INewsStoriesService>();

            var items = new List<NewsStoryDto>
            {
                new(1, "A", "http://a", "u", 0, 1),
                new(2, "B", null, "v", 0, 2),
            };

            service.Setup(s => s.GetNewestAsync(It.IsAny<PagedSearchRequestDto>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((items, items.Count()));

            var controller = new NewsStoriesController(service.Object);

            PagedSearchRequestDto requestDto = new PagedSearchRequestDto()
            {
                PageNumber = 1,
                PageSize = 10,
                Search = null
            };

            var result = await controller.GetNewest(requestDto, ct: default);
            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic body = ok.Value!;

            Assert.Equal(2, (int)body.Total);
            Assert.Equal(2, ((IEnumerable<NewsStoryDto>)body.Items).ToList().Count);
        }

        [Fact]
        public async Task GetById_ReturnsStory()
        {
            var service = new Mock<INewsStoriesService>();

            NewsStoryDto newsStory = new NewsStoryDto(1, "A", "http://a", "u", 0, 1);

            service.Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(newsStory);

            var controller = new NewsStoriesController(service.Object);

            var result = await controller.GetById(1, ct: default);
            var ok = Assert.IsType<OkObjectResult>(result);
            dynamic body = ok.Value!;

            Assert.Equal(1, body.Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound()
        {
            var service = new Mock<INewsStoriesService>();

            service.Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((NewsStoryDto?)null);

            var controller = new NewsStoriesController(service.Object);

            var result = await controller.GetById(9990, CancellationToken.None);

            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }
    }
}