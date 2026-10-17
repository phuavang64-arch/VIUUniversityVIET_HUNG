using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/banners")]
    [Authorize(Roles = "Admin")]
    public class BannersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public BannersController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var banners = await _db.Banners.Where(b => !b.IsDeleted).OrderBy(b => b.DisplayOrder).ToListAsync();
            return View(banners);
        }

        [HttpGet("create")]
        public IActionResult Create() => View(new Banner { DisplayOrder = 1 });

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner model, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    model.ImageUrl = await SaveBannerImageAsync(imageFile);
                    ModelState.Remove(nameof(Banner.ImageUrl));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (!ModelState.IsValid) return View(model);

            model.CreatedAt = DateTime.UtcNow;
            _db.Banners.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm banner thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner == null || banner.IsDeleted) return NotFound();
            return View(banner);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner model, IFormFile? imageFile)
        {
            if (id != model.Id) return BadRequest();

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    model.ImageUrl = await SaveBannerImageAsync(imageFile);
                    ModelState.Remove(nameof(Banner.ImageUrl));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (!ModelState.IsValid) return View(model);

            var existing = await _db.Banners.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            existing.Title = model.Title;
            existing.Description = model.Description;
            if (!string.IsNullOrWhiteSpace(model.ImageUrl))
                existing.ImageUrl = model.ImageUrl;
            existing.ButtonText = model.ButtonText;
            existing.ButtonUrl = model.ButtonUrl;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật banner thành công.";
            return RedirectToAction(nameof(Index));
        }

        private Task<string> SaveBannerImageAsync(IFormFile file) =>
            ImageUploadHelper.SaveAsync(file, _env.WebRootPath, "banners");

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
