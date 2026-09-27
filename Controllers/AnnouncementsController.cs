using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    [Route("thong-bao")]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _announcementService;
        private const int PageSize = 10;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _announcementService.GetPublishedAsync(page < 1 ? 1 : page, PageSize);
            return View(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _announcementService.GetByIdAsync(id);
            if (item == null || !item.IsPublished) return NotFound();
            return View(item);
        }
    }
}
