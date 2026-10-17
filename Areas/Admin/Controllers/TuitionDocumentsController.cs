using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    /// <summary>
    /// Quản trị chuyên mục Học phí.
    ///
    /// Khác hai chuyên mục còn lại ở chỗ mỗi bài là MỘT VĂN BẢN (thông báo, quyết
    /// định, công văn) chứ không phải một bài tin: vì vậy form có thêm số ký hiệu,
    /// ngày ban hành, năm học áp dụng và tệp PDF đính kèm. Website không trình
    /// bày học phí như một bảng giá cố định cho mọi năm — mỗi văn bản ghi rõ năm
    /// áp dụng của nó.
    /// </summary>
    [Area("Admin")]
    [Route("admin/hoc-phi")]
    [Authorize(Roles = "Admin,Editor")]
    public class TuitionDocumentsController : SectionContentControllerBase
    {
        public TuitionDocumentsController(
            INewsService newsService, ApplicationDbContext db, IWebHostEnvironment env)
            : base(newsService, db, env)
        {
        }

        protected override string Section => ContentSection.HocPhi;
        protected override string Label => "Văn bản học phí";
        protected override string UploadFolder => "tuition";

        protected override Task PrepareFormAsync()
        {
            // Danh sách năm học gợi ý cho ô nhập — chỉ là gợi ý, không ép giá trị.
            ViewBag.SuggestedYears = Enumerable.Range(DateTime.Now.Year - 1, 5)
                .Select(y => $"{y} - {y + 1}")
                .ToList();
            return Task.CompletedTask;
        }
    }
}