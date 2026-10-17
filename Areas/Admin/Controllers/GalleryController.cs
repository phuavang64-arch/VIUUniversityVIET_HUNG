using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/gallery")]
    [Authorize(Roles = "Admin,Editor")]
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public GalleryController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET admin/gallery
        [HttpGet("")]
        public async Task<IActionResult> Index(string? album = null)
        {
            var query = _db.GalleryImages.Where(g => !g.IsDeleted);
            if (!string.IsNullOrWhiteSpace(album))
                query = query.Where(g => g.Album == album);

            var images = await query.OrderBy(g => g.DisplayOrder).ThenByDescending(g => g.CreatedAt).ToListAsync();
            ViewBag.Albums = await _db.GalleryImages
                .Where(g => !g.IsDeleted && g.Album != null)
                .Select(g => g.Album!)
                .Distinct()
                .ToListAsync();
            ViewBag.SelectedAlbum = album;
            return View(images);
        }

        // GET admin/gallery/create
        [HttpGet("create")]
        public IActionResult Create() => View(new GalleryImage());

        // POST admin/gallery/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GalleryImage model, IList<IFormFile>? imageFiles)
        {
            // Cho phép upload nhiều ảnh cùng lúc
            if (imageFiles != null && imageFiles.Any(f => f.Length > 0))
            {
                var uploadedCount = 0;
                foreach (var file in imageFiles.Where(f => f.Length > 0))
                {
                    var url = await SaveImageAsync(file);
                    var img = new GalleryImage
                    {
                        ImageUrl = url,
                        Caption = model.Caption,
                        Album = model.Album,
                        DisplayOrder = model.DisplayOrder + uploadedCount
                    };
                    _db.GalleryImages.Add(img);
                    uploadedCount++;
                }
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm {uploadedCount} hình ảnh vào thư viện.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                _db.GalleryImages.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã thêm hình ảnh bằng URL.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Vui lòng upload ảnh hoặc nhập URL ảnh.");
            return View(model);
        }

        // GET admin/gallery/edit/{id}
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var img = await _db.GalleryImages.FindAsync(id);
            if (img == null || img.IsDeleted) return NotFound();
            return View(img);
        }

        // POST admin/gallery/edit/{id}
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GalleryImage model, IFormFile? imageFile)
        {
            var existing = await _db.GalleryImages.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            existing.Caption = model.Caption;
            existing.Album = model.Album;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            if (imageFile != null && imageFile.Length > 0)
                existing.ImageUrl = await SaveImageAsync(imageFile);
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
                existing.ImageUrl = model.ImageUrl;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật hình ảnh.";
            return RedirectToAction(nameof(Index));
        }

        // POST admin/gallery/delete/{id}
        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var img = await _db.GalleryImages.FindAsync(id);
            if (img == null) return NotFound();
            img.IsDeleted = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa hình ảnh.";
            return RedirectToAction(nameof(Index));
        }

        private Task<string> SaveImageAsync(IFormFile file) =>
            ImageUploadHelper.SaveAsync(file, _env.WebRootPath, "gallery");
    }
}
