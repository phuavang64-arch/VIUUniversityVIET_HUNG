using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/events")]
    [Authorize(Roles = "Admin,Editor")]
    public class EventsController : Controller
    {
        private readonly IEventService _service;
        public EventsController(IEventService service) => _service = service;

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _service.GetAllForAdminAsync(page < 1 ? 1 : page, 10);
            return View(result);
        }

        [HttpGet("create")]
        public IActionResult Create() => View(new Event());

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model)
        {
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            if (!ModelState.IsValid) return View(model);
            await _service.CreateAsync(model);
            TempData["Success"] = "Đã thêm sự kiện.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model)
        {
            if (id != model.Id) return BadRequest();
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);
            if (!ModelState.IsValid) return View(model);
            await _service.UpdateAsync(model);
            TempData["Success"] = "Đã cập nhật sự kiện.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Đã xóa sự kiện.";
            return RedirectToAction(nameof(Index));
        }
    }
}
