using Microsoft.AspNetCore.Mvc;

namespace NewsArticles.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsStoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
