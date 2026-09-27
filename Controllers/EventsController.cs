using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    [Route("su-kien")]
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private const int PageSize = 9;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _eventService.GetPublishedAsync(page < 1 ? 1 : page, PageSize);
            return View(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var item = await _eventService.GetBySlugAsync(slug);
            if (item == null) return NotFound();
            return View(item);
        }
    }
}
