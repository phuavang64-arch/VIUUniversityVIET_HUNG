using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/news")]
    [Authorize(Roles = "Admin,Editor")]
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public NewsController(INewsService newsService, ApplicationDbContext db, IWebHostEnvironment env)
        {
            _newsService = newsService;
            _db = db;
            _env = env;
        }

        // GET admin/news
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null)
        {
            var result = await _newsService.GetAllForAdminAsync(page < 1 ? 1 : page, 10, keyword);
            ViewBag.Keyword = keyword;
            return View(result);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            return View(new News { IsPublished = true });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News model, IFormFile? thumbnailFile)
        {
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            // 0 do <select> gửi lên khi người dùng chọn "-- Không phân loại --".
            // Cột đã nullable nên 0 phải được đổi thành null, nếu không sẽ vi phạm
            // khoá ngoại và lỗi 500 khi lưu.
            if (model.CategoryId is 0)
                model.CategoryId = null;

            // Clear navigation validation noise
            ModelState.Remove(nameof(News.Category));
            ModelState.Remove(nameof(News.Author));

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }

            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                try
                {
                    model.Thumbnail = await SaveThumbnailAsync(thumbnailFile);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    await PopulateCategoriesAsync();
                    return View(model);
                }
            }

            model.AuthorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (model.IsPublished && model.PublishedAt == null)
                model.PublishedAt = DateTime.UtcNow;

            await _newsService.CreateAsync(model);
            TempData["Success"] = "Đã đăng tin tức thành công. Tin sẽ hiển thị trên trang Tin tức nếu đã chọn Đăng ngay.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var news = await _newsService.GetByIdAsync(id);
            if (news == null) return NotFound();
            await PopulateCategoriesAsync();
            return View(news);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News model, IFormFile? thumbnailFile)
        {
            if (id != model.Id) return BadRequest();

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            if (model.CategoryId is 0)
                model.CategoryId = null;

            ModelState.Remove(nameof(News.Category));
            ModelState.Remove(nameof(News.Author));

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }

            var existing = await _newsService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                try
                {
                    existing.Thumbnail = await SaveThumbnailAsync(thumbnailFile);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    await PopulateCategoriesAsync();
                    return View(model);
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Thumbnail))
            {
                existing.Thumbnail = model.Thumbnail;
            }

            // UpdateAsync owns field mapping and sanitising; assigning them here as well
            // duplicated the logic and risked the two copies drifting apart.
            existing.Title = model.Title;
            existing.Slug = model.Slug;
            existing.Summary = model.Summary;
            existing.Content = model.Content;
            existing.CategoryId = model.CategoryId;
            existing.IsPublished = model.IsPublished;

            await _newsService.UpdateAsync(existing);
            TempData["Success"] = "Đã cập nhật tin tức.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _newsService.DeleteAsync(id);
            TempData["Success"] = "Đã xóa tin tức.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("toggle-publish/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id, bool isPublished)
        {
            await _newsService.SetPublishStateAsync(id, isPublished);
            TempData["Success"] = isPublished ? "Đã xuất bản tin." : "Đã chuyển tin về trạng thái nháp.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategoriesAsync()
        {
            ViewBag.Categories = await _db.NewsCategories.Where(c => c.IsActive).ToListAsync();
        }

        private Task<string> SaveThumbnailAsync(IFormFile file) =>
            ImageUploadHelper.SaveAsync(file, _env.WebRootPath, "news");
    }
}
