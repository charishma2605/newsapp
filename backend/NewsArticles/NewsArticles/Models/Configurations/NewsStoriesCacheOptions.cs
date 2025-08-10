namespace NewsArticles.Models.Configurations
{
    public class NewsStoriesCacheOptions
    {
        public string BaseUrl { get; set; } 
        public int CacheSecondsForIdList { get; set; }
        public int CacheSecondsForStory { get; set; } 
    }
}
