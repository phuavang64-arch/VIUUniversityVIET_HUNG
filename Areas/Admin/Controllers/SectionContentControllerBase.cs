using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    /// <summary>
    /// Khung CRUD dùng chung cho các chuyên mục bài viết.
    ///
    /// Tin tức, Nghiên cứu khoa học, Hợp tác và Học phí cùng nằm trong bảng
    /// <see cref="News"/> và chỉ khác nhau ở <c>News.Section</c> (xem
    /// <see cref="ContentSection"/>). Viết một lớp cơ sở rồi mỗi chuyên mục chỉ
    /// khai báo route + tên hiển thị tránh bốn bản controller gần như giống hệt nhau.
    ///
    /// <c>[ValidateAntiForgeryToken]</c> KHÔNG đặt ở cấp lớp mà gắn riêng vào từng
    /// action POST. Đặt ở cấp lớp khiến bộ lọc chạy cả cho các action GET, và các
    /// trang danh sách / mở form (mở bằng <c>GET</c>, không có token) trả về 400.
    /// </summary>
    [Authorize(Roles = "Admin,Editor")]
    public abstract class SectionContentControllerBase : Controller
    {
        protected const int PageSize = 10;

        private readonly INewsService _newsService;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        protected SectionContentControllerBase(
            INewsService newsService, ApplicationDbContext db, IWebHostEnvironment env)
        {
            _newsService = newsService;
            _db = db;
            _env = env;
        }

        /// <summary>Giá trị của <see cref="ContentSection"/> mà lớp con phục vụ.</summary>
        protected abstract string Section { get; }

        /// <summary>Tên hiển thị ở tiêu đề trang và thông báo sau khi lưu.</summary>
        protected abstract string Label { get; }

        /// <summary>Thư mục con trong wwwroot/uploads cho ảnh của chuyên mục này.</summary>
        protected abstract string UploadFolder { get; }

        // ---- Danh sách ----
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null)
        {
            var result = await _newsService.GetAllForAdminBySectionAsync(
                Section, page < 1 ? 1 : page, PageSize, keyword);

            ViewBag.Keyword = keyword;
            ViewBag.SectionLabel = Label;
            return View(result);
        }

        // ---- Thêm ----
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            await PrepareFormAsync();
            return View("Form", new News { IsPublished = true, Section = Section });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("create")]
        public async Task<IActionResult> Create(News model, IFormFile? thumbnailFile,
                                                 IFormFile? attachmentFile)
        {
            model.Section = Section;

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            model.CategoryId = model.CategoryId is 0 ? null : model.CategoryId;

            ModelState.Remove(nameof(News.Category));
            ModelState.Remove(nameof(News.Author));

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                await PrepareFormAsync();
                return View("Form", model);
            }

            if (!await TrySaveUploadsAsync(model, thumbnailFile, attachmentFile))
            {
                await PopulateCategoriesAsync();
                await PrepareFormAsync();
                return View("Form", model);
            }

            model.AuthorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (model.IsPublished && model.PublishedAt == null)
                model.PublishedAt = DateTime.UtcNow;

            await _newsService.CreateAsync(model);
            TempData["Success"] = $"Đã đăng {Label.ToLower()} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Sửa ----
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _newsService.GetByIdAsync(id);
            if (article == null || article.Section != Section)
                return NotFound();

            await PopulateCategoriesAsync();
            await PrepareFormAsync();
            return View("Form", article);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, News model, IFormFile? thumbnailFile,
                                              IFormFile? attachmentFile)
        {
            if (id != model.Id)
                return BadRequest();

            model.Section = Section;

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = SlugHelper.GenerateSlug(model.Title);

            model.CategoryId = model.CategoryId is 0 ? null : model.CategoryId;

            ModelState.Remove(nameof(News.Category));
            ModelState.Remove(nameof(News.Author));

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                await PrepareFormAsync();
                return View("Form", model);
            }

            var existing = await _newsService.GetByIdAsync(id);
            if (existing == null || existing.Section != Section)
                return NotFound();

            existing.Title = model.Title;
            existing.Slug = model.Slug;
            existing.Summary = model.Summary;
            existing.Content = model.Content;
            existing.CategoryId = model.CategoryId;
            existing.IsPublished = model.IsPublished;
            existing.DocumentNumber = model.DocumentNumber;
            existing.IssuedDate = model.IssuedDate;
            existing.AcademicYear = model.AcademicYear;
            existing.AttachmentName = model.AttachmentName;
            existing.VideoUrl = model.VideoUrl;

            if (!await TrySaveUploadsAsync(existing, thumbnailFile, attachmentFile, model))
            {
                await PopulateCategoriesAsync();
                await PrepareFormAsync();
                return View("Form", model);
            }

            await _newsService.UpdateAsync(existing);
            TempData["Success"] = $"Đã cập nhật {Label.ToLower()}.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Xoá ----
        [ValidateAntiForgeryToken]
        [HttpPost("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _newsService.GetByIdAsync(id);
            if (article == null || article.Section != Section)
                return NotFound();

            await _newsService.DeleteAsync(id);
            TempData["Success"] = $"Đã xoá {Label.ToLower()}.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Nháp / đã đăng ----
        [ValidateAntiForgeryToken]
        [HttpPost("toggle-publish/{id:int}")]
        public async Task<IActionResult> TogglePublish(int id, bool isPublished)
        {
            var article = await _newsService.GetByIdAsync(id);
            if (article == null || article.Section != Section)
                return NotFound();

            await _newsService.SetPublishStateAsync(id, isPublished);
            TempData["Success"] = isPublished
                ? "Đã xuất bản."
                : "Đã chuyển về trạng thái nháp.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Xem trước ----
        // Dùng đúng view chi tiết của website công khai, nên "xem trước" không thể
        // lệch với thứ người đọc thực sự thấy sau khi bài được đăng.
        [HttpGet("preview/{id:int}")]
        public async Task<IActionResult> Preview(int id)
        {
            var article = await _db.News
                .Include(n => n.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && n.Section == Section && !n.IsDeleted);

            if (article == null)
                return NotFound();

            // Nội dung trong DB đã qua HtmlSanitizer; render bằng đúng view thật.
            ViewBag.PreviewMode = true;
            ViewData["Title"] = $"Xem trước: {article.Title}";
            return View("~/Views/Shared/Section/_Details.cshtml", article);
        }

        private async Task PopulateCategoriesAsync()
        {
            ViewBag.Categories = await _db.NewsCategories.Where(c => c.IsActive).ToListAsync();
        }

        /// <summary>Chuẩn bị dữ liệu dùng chung cho form, lớp con có thể bổ sung.</summary>
        protected virtual Task PrepareFormAsync() => Task.CompletedTask;

        /// <summary>
        /// Lưu ảnh và tệp đính kèm. Trả về false khi có lỗi — thông báo đã được
        /// đẩy vào ModelState, view sẽ hiển thị lại form kèm thông báo.
        /// </summary>
        private async Task<bool> TrySaveUploadsAsync(
            News target, IFormFile? thumbnailFile, IFormFile? attachmentFile, News? posted = null)
        {
            if (thumbnailFile is { Length: > 0 })
            {
                try
                {
                    target.Thumbnail = await ImageUploadHelper.SaveAsync(
                        thumbnailFile, _env.WebRootPath, UploadFolder);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Ảnh đại diện: " + ex.Message);
                    return false;
                }
            }
            else if (posted != null && !string.IsNullOrWhiteSpace(posted.Thumbnail))
            {
                // Không tải file mới: giữ đường dẫn đã nhập tay.
                target.Thumbnail = posted.Thumbnail;
            }

            if (attachmentFile is { Length: > 0 })
            {
                try
                {
                    target.AttachmentUrl = await DocumentUploadHelper.SavePdfAsync(
                        attachmentFile, _env.WebRootPath, UploadFolder);
                    target.AttachmentName = attachmentFile.FileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Tệp đính kèm: " + ex.Message);
                    return false;
                }
            }

            return true;
        }
    }
}