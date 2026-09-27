using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/announcements")]
    [Authorize(Roles = "Admin,Editor")]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _service;
        private readonly ApplicationDbContext _db;

        public AnnouncementsController(IAnnouncementService service, ApplicationDbContext db)
        {
            _service = service;
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _service.GetAllForAdminAsync(page < 1 ? 1 : page, 10);
            return View(result);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            return View(new Announcement());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }
            await _service.CreateAsync(model);
            TempData["Success"] = "Đã thêm thông báo.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            await PopulateCategoriesAsync();
            return View(item);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }
            await _service.UpdateAsync(model);
            TempData["Success"] = "Đã cập nhật thông báo.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            TempData["Success"] = "Đã xóa thông báo.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategoriesAsync()
        {
            ViewBag.Categories = await _db.AnnouncementCategories.Where(c => c.IsActive).ToListAsync();
        }
    }
}
