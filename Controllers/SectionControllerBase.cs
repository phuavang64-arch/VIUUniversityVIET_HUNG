using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Controllers
{
    /// <summary>
    /// Khung chung cho các trang danh sách/chi tiết của chuyên mục bài viết
    /// (Nghiên cứu khoa học, Hợp tác, Học phí).
    ///
    /// Cùng cơ chế với /tin-tuc: cùng bảng <see cref="News"/>, cùng phân trang,
    /// cùng đếm lượt đọc, cùng kiểu bài trên view. Chuyên mục khác nhau duy nhất
    /// ở <see cref="ContentSection"/> và ở các ô riêng của văn bản học phí.
    /// </summary>
    public abstract class SectionControllerBase : Controller
    {
        private const int PageSize = 9;

        private readonly INewsService _newsService;

        protected SectionControllerBase(INewsService newsService) => _newsService = newsService;

        /// <summary>Chuyên mục phục vụ.</summary>
        protected abstract string Section { get; }

        /// <summary>Tiền tố đường dẫn, ví dụ "/nghien-cuu".</summary>
        protected abstract string BasePath { get; }

        protected abstract string PageTitle { get; }
        protected abstract string PageDescription { get; }

        /// <summary>Ảnh phủ nền đầu trang.</summary>
        protected abstract string PagePhoto { get; }

        /// <summary>
        /// Khối trình bày riêng của chuyên mục (số ký hiệu, năm học, tệp PDF) trên
        /// cả trang danh sách lẫn trang chi tiết.
        /// </summary>
        protected virtual bool ShowTuitionFields => false;

        /// <summary>Nội dung tĩnh hiển thị phía trên danh sách bài viết.</summary>
        protected virtual List<Dictionary<string, object>> LeadSections => new();

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null)
        {
            var result = await _newsService.GetPublishedBySectionAsync(
                Section, page < 1 ? 1 : page, PageSize, keyword);

            ViewBag.SectionKey = Section;
            ViewBag.BasePath = BasePath;
            ViewBag.PageTitle = PageTitle;
            ViewBag.PageDescription = PageDescription;
            ViewBag.PagePhoto = PagePhoto;
            ViewBag.ShowTuitionFields = ShowTuitionFields;
            ViewBag.SectionLabel = ContentSection.Label(Section);
            ViewBag.LeadSections = LeadSections;
            ViewBag.Keyword = keyword;

            ViewData["Title"] = PageTitle;
            ViewData["Description"] = PageDescription;

            return View("~/Views/Shared/Section/_Index.cshtml", result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var article = await _newsService.GetBySlugInSectionAsync(slug, Section);
            if (article == null)
                return NotFound();

            ViewBag.BasePath = BasePath;
            ViewBag.ShowTuitionFields = ShowTuitionFields;
            ViewBag.SectionLabel = ContentSection.Label(Section);
            ViewBag.Related = await _newsService.GetRelatedInSectionAsync(Section, article.Id, 3);

            ViewData["Title"] = article.Title;
            ViewData["Description"] = article.Summary;
            if (ViewBag.PreviewMode == null)
                ViewData["NoIndex"] = true;   // tránh trùng khi bài đã có mục khác

            return View("~/Views/Shared/Section/_Details.cshtml", article);
        }
    }
}