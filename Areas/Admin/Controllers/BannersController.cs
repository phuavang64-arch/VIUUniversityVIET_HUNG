using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/banners")]
    [Authorize(Roles = "Admin")]
    public class BannersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BannersController(ApplicationDbContext db) => _db = db;

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var banners = await _db.Banners.Where(b => !b.IsDeleted).OrderBy(b => b.DisplayOrder).ToListAsync();
            return View(banners);
        }

        [HttpGet("create")]
        public IActionResult Create() => View(new Banner());

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Banners.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm banner.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            model.UpdatedAt = DateTime.UtcNow;
            _db.Banners.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật banner.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner != null)
            {
                banner.IsDeleted = true;
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Đã xóa banner.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("toggle/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner != null)
            {
                banner.IsActive = !banner.IsActive;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
