using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    [Route("tin-tuc")]
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;
        private const int PageSize = 9;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        // GET /tin-tuc?page=1&keyword=...&categoryId=...
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null, int? categoryId = null)
        {
            var result = await _newsService.GetPublishedAsync(page < 1 ? 1 : page, PageSize, keyword, categoryId);
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            return View(result);
        }

        // GET /tin-tuc/{slug}
        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var news = await _newsService.GetBySlugAsync(slug);
            if (news == null) return NotFound();
            return View(news);
        }
    }
}
