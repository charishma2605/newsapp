namespace NewsArticles.Models
{
    public class NewestStoriesResponseDto
    {
        public int Page {  get; set; }
        public int PageSize {  get; set; }
        public int Total { get; set; }
        public IEnumerable<NewsStoryDto> Items { get; set; }
    }
}
