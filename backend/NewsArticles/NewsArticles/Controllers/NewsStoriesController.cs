using Microsoft.AspNetCore.Mvc;
using NewsArticles.Models;
using NewsArticles.Services.Interfaces;

namespace NewsArticles.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsStoriesController : Controller
    {
        private readonly INewsStoriesService _service;

        public NewsStoriesController(INewsStoriesService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get newest News stories with paging and optional title search.
        /// </summary>
        [HttpGet("newest")]
        public async Task<IActionResult> GetNewest([FromBody] PagedSearchRequestDto requestDto, CancellationToken ct = default)
        {
            var (items, total) = await _service.GetNewestAsync(requestDto, ct);
            return Ok(new {  total, items });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var item = await _service.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }
    }
}
