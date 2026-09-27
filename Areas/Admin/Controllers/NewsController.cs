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

        public NewsController(INewsService newsService, ApplicationDbContext db)
        {
            _newsService = newsService;
            _db = db;
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
            return View(new News());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News model)
        {
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }

            model.AuthorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _newsService.CreateAsync(model);
            TempData["Success"] = "Đã thêm tin tức thành công.";
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
        public async Task<IActionResult> Edit(int id, News model)
        {
            if (id != model.Id) return BadRequest();

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(model);
            }

            await _newsService.UpdateAsync(model);
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
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategoriesAsync()
        {
            ViewBag.Categories = await _db.NewsCategories.Where(c => c.IsActive).ToListAsync();
        }
    }
}
