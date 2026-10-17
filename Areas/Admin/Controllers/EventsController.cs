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
        private readonly IWebHostEnvironment _env;

        public EventsController(IEventService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _service.GetAllForAdminAsync(page < 1 ? 1 : page, 10);
            return View(result);
        }

        [HttpGet("create")]
        public IActionResult Create() => View(new Event { StartAt = DateTime.UtcNow.AddDays(7), IsPublished = true });

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    model.ImageUrl = await SaveEventImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (!ModelState.IsValid) return View(model);
            await _service.CreateAsync(model);
            TempData["Success"] = "Đã thêm sự kiện thành công.";
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
        public async Task<IActionResult> Edit(int id, Event model, IFormFile? imageFile)
        {
            if (id != model.Id) return BadRequest();
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    model.ImageUrl = await SaveEventImageAsync(imageFile);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (!ModelState.IsValid) return View(model);
            await _service.UpdateAsync(model);
            TempData["Success"] = "Đã cập nhật sự kiện thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveEventImageAsync(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Chỉ chấp nhận file ảnh định dạng JPG, PNG, WEBP.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "events");
            Directory.CreateDirectory(folder);

            var fileName = $"event_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);
            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);
            return $"/uploads/events/{fileName}";
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
