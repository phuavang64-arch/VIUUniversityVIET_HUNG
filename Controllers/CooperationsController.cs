using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    /// <summary>
    /// Chuyên mục Hợp tác quốc tế và doanh nghiệp: /hop-tac và /hop-tac/{slug}.
    ///
    /// Đường dẫn /hop-tac trước đây do HomeController.International phục vụ. Action đó
    /// đã bỏ để tránh trùng route; phần nội dung giới thiệu vốn có của trang được
    /// chuyển vào <see cref="LeadSections"/> nên không mất gì.
    /// </summary>
    [AllowAnonymous]
    [Route("hop-tac")]
    public class CooperationsController : SectionControllerBase
    {
        public CooperationsController(INewsService newsService) : base(newsService)
        {
        }

        protected override string Section => ContentSection.HopTac;
        protected override string BasePath => "/hop-tac";
        protected override string PageTitle => "Hợp tác quốc tế và doanh nghiệp";
        protected override string PagePhoto => "/images/viu/viu-signing.jpg";
        protected override string PageDescription =>
            "Mạng lưới hợp tác quốc tế, ký kết hợp tác, trao đổi học thuật và thực tập " +
            "của Trường Đại học Công nghiệp Việt - Hung.";

        protected override List<Dictionary<string, object>> LeadSections => new()
        {
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Hợp tác",
                ["title"] = "Quan hệ hợp tác",
                ["image"] = "/images/viu/viu-signing.jpg",
                ["imageAlt"] = "Lễ ký kết hợp tác toàn diện tại nhà trường",
                ["body"] = "<p>Trường duy trì hợp tác đào tạo với các cơ sở trong nước và với các đối tác " +
                            "tại Hungary, đồng thời mở rộng liên kết với các doanh nghiệp để phục vụ " +
                            "thực tập và tuyển dụng sinh viên.</p>" +
                            "<p>Các thỏa thuận hợp tác được triển khai dưới nhiều hình thức: ký kết biên bản " +
                            "ghi nhớ, đồng kiểm soát chương trình đào tạo, cử giảng viên thực hành và tổ chức " +
                            "các chương trình giao lưu học thuật.</p>"
            },
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Doanh nghiệp",
                ["title"] = "Hợp tác với doanh nghiệp",
                ["alt"] = true,
                ["image"] = "/images/viu/viu-partnership.jpg",
                ["imageAlt"] = "Hoạt động hợp tác giữa nhà trường và doanh nghiệp",
                ["body"] = "<p>Thông qua các thỏa thuận ký kết, doanh nghiệp tham gia đồng kiểm soát " +
                            "chương trình đào tạo, cử giảng viên thực hành, tiếp nhận sinh viên thực tập và " +
                            "giới thiệu cơ hội việc làm sau tốt nghiệp.</p>" +
                            "<p>Mô hình này giúp sinh viên tiếp cận môi trường làm việc thật ngay từ khi còn " +
                            "ngồi trên ghế nhà trường, đồng thời giúp doanh nghiệp tiếp cận nguồn nhân lực " +
                            "được đào tạo đúng nhu cầu.</p>"
            },
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Quốc tế",
                ["title"] = "Hợp tác quốc tế",
                ["image"] = "/images/viu/viu-hoi-tnvn.jpg",
                ["imageAlt"] = "Sinh viên tham gia hoạt động Hội Thanh niên Việt Nam",
                ["body"] = "<p>Chương trình đào tạo được xây dựng theo hợp tác Việt Nam - Hungary, kết hợp " +
                            "kinh nghiệm của hai nước trong đào tạo nghề và kỹ thuật công nghiệp. Sinh viên có " +
                            "cơ hội giao lưu với sinh viên và giảng viên từ các đối tác nước ngoài.</p>" +
                            "<p>Bên cạnh Hungary, Trường mở rộng quan hệ với các đối tác tại châu Âu, châu Á " +
                            "và các tổ chức giáo dục quốc tế trong lĩnh vực đào tạo kỹ thuật và công nghệ.</p>"
            }
        };
    }
}