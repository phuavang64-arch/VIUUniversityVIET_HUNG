using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    /// <summary>Chuyên mục Nghiên cứu khoa học: /nghien-cuu và /nghien-cuu/{slug}.</summary>
    [AllowAnonymous]
    [Route("nghien-cuu")]
    public class ResearchController : SectionControllerBase
    {
        public ResearchController(INewsService newsService) : base(newsService)
        {
        }

        protected override string Section => ContentSection.NghiênCứu;
        protected override string BasePath => "/nghien-cuu";
        protected override string PageTitle => "Nghiên cứu khoa học";
        protected override string PagePhoto => "/images/viu/viu-khoa-dien.jpg";
        protected override string PageDescription =>
            "Các bài viết về hoạt động nghiên cứu khoa học, chuyển giao công nghệ và " +
            "những đề tài ứng dụng của nhà trường.";

        protected override List<Dictionary<string, object>> LeadSections => new()
        {
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Nghiên cứu",
                ["title"] = "Hoạt động nghiên cứu khoa học",
                ["image"] = "/images/viu/viu-khoa-cntt.jpg",
                ["imageAlt"] = "Sinh viên thực hành tại phòng máy tính của Khoa Công nghệ thông tin",
                ["body"] = "<p>Hoạt động nghiên cứu khoa học tại Trường được tổ chức theo hướng phục vụ " +
                            "công nghiệp hoá, hiện đại hoá và phát triển thực tế của doanh nghiệp.</p>" +
                            "<p>Các nhóm nghiên cứu tập trung vào các lĩnh vực công nghệ thông tin, điện tử, " +
                            "tự động hoá và cơ khí ứng dụng, với các đề tài được triển khai từ cấp cơ sở " +
                            "đến cấp Bộ.</p>"
            },
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Chuyển giao",
                ["title"] = "Chuyển giao công nghệ",
                ["alt"] = true,
                ["image"] = "/images/viu/viu-signing.jpg",
                ["imageAlt"] = "Lễ ký kết hợp tác chuyển giao công nghệ tại nhà trường",
                ["body"] = "<p>Kết quả nghiên cứu không chỉ dừng ở bài báo và luận văn mà được Trường " +
                            "đẩy mạnh chuyển giao vào thực tiễn sản xuất, thông qua các thỏa thuận hợp tác " +
                            "với doanh nghiệp và các đối tác trong nước, quốc tế.</p>"
            },
            new Dictionary<string, object>
            {
                ["eyebrow"] = "Sinh viên",
                ["title"] = "Sinh viên tham gia nghiên cứu",
                ["image"] = "/images/viu/viu-students-group.jpg",
                ["imageAlt"] = "Nhóm sinh viên làm đồ án nghiên cứu",
                ["body"] = "<p>Sinh viên từ năm thứ hai được khuyến khích tham gia các nhóm nghiên cứu " +
                            "của giảng viên, thực hiện đề tài khoa học cấp Khoa và các cuộc thi nghiên cứu " +
                            "khoa học sinh viên trong và ngoài nước.</p>"
            }
        };
    }
}