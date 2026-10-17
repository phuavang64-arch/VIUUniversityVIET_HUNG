using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    /// <summary>
    /// Chuyên mục Học phí: /hoc-phi và /hoc-phi/{slug}.
    ///
    /// Cố tình KHÔNG trình bày học phí như một bảng giá cố định cho mọi năm. Mỗi bài
    /// là một văn bản/thông báo do Ban Tuyển sinh đăng từ văn bản chính thức, kèm số
    /// ký hiệu, ngày ban hành, năm học áp dụng và tệp PDF nếu có. Khi trường đăng
    /// văn bản mới, trang hiển thị ngay mà không phải sửa mã nguồn.
    /// </summary>
    [AllowAnonymous]
    [Route("hoc-phi")]
    public class TuitionController : SectionControllerBase
    {
        public TuitionController(INewsService newsService) : base(newsService)
        {
        }

        protected override string Section => ContentSection.HocPhi;
        protected override string BasePath => "/hoc-phi";
        protected override string PageTitle => "Học phí";
        protected override string PagePhoto => "/images/viu/viu-hall.jpg";
        protected override string PageDescription =>
            "Văn bản và thông báo về học phí của Trường Đại học Công nghiệp Việt - Hung, " +
            "ghi rõ năm học áp dụng và tệp đính kèm.";

        protected override bool ShowTuitionFields => true;

        protected override List<Dictionary<string, object>> LeadSections => new()
        {
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Lưu ý",
                ["title"] = "Học phí được công bố theo văn bản, theo từng năm",
                ["body"] = "<p>Mức học phí thay đổi theo từng năm tuyển sinh và tùy phương thức xét tuyển. " +
                            "Trang này liệt kê các văn bản và thông báo do nhà trường ban hành; mỗi văn bản ghi rõ " +
                            "số ký hiệu, ngày ban hành và năm học áp dụng.</p>" +
                            "<p>Để biết chính xác mức học phí của năm mình dự tuyển, vui lòng xem văn bản có " +
                            "năm học tương ứng, hoặc liên hệ Hotline <strong>024.3383.8345</strong>.</p>"
            }
        };
    }
}