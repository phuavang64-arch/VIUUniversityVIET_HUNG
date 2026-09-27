using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IAcademicService _academicService;
        private readonly IAdmissionService _admissionService;
        private readonly ApplicationDbContext _db;

        public HomeController(
            IHomeService homeService,
            IAcademicService academicService,
            IAdmissionService admissionService,
            ApplicationDbContext db)
        {
            _homeService = homeService;
            _academicService = academicService;
            _admissionService = admissionService;
            _db = db;
        }

        // GET /
        public async Task<IActionResult> Index()
        {
            var vm = await _homeService.BuildHomeViewModelAsync();
            return View(vm);
        }

        // GET /gioi-thieu
        [HttpGet("gioi-thieu")]
        public async Task<IActionResult> About()
        {
            var campuses = await _academicService.GetAllCampusesAsync();
            ViewBag.Campuses = campuses;
            ViewData["Title"] = "Giới thiệu Trường Đại học Công nghiệp Việt - Hung";
            ViewData["Description"] = "Lịch sử hình thành, sứ mạng, tầm nhìn và cơ sở vật chất của Trường Đại học Công nghiệp Việt - Hung (VIU).";
            return View();
        }

        // GET /lien-he
        [HttpGet("lien-he")]
        public async Task<IActionResult> Contact()
        {
            var campuses = await _academicService.GetAllCampusesAsync();
            var vm = new ContactViewModel
            {
                Campuses = campuses,
                Message = new ContactMessage()
            };

            ViewData["Title"] = "Liên hệ";
            ViewData["Description"] = "Thông tin liên hệ các cơ sở đào tạo và biểu mẫu gửi câu hỏi tới VIU.";
            return View(vm);
        }

        // POST /lien-he
        [HttpPost("lien-he")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(ContactViewModel vm)
        {
            if (ModelState.IsValid)
            {
                await _admissionService.SubmitContactMessageAsync(vm.Message);
                TempData["Success"] = "Cảm ơn bạn đã gửi liên hệ! Nhà trường đã tiếp nhận và sẽ phản hồi sớm nhất qua email/SĐT.";
                return RedirectToAction(nameof(Contact));
            }

            vm.Campuses = await _academicService.GetAllCampusesAsync();
            TempData["Error"] = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
            return View(nameof(Contact), vm);
        }

        // GET /nghien-cuu
        [HttpGet("nghien-cuu")]
        public IActionResult Research()
        {
            ViewData["Title"] = "Nghiên cứu khoa học & Chuyển giao công nghệ";
            ViewData["Description"] = "Hoạt động nghiên cứu khoa học, chuyển giao công nghệ và các đề tài ứng dụng tại Trường Đại học Công nghiệp Việt - Hung.";
            return View();
        }

        // GET /hop-tac
        [HttpGet("hop-tac")]
        public async Task<IActionResult> International()
        {
            var partners = await _db.Partners.Where(p => !p.IsDeleted && p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            ViewBag.Partners = partners;
            ViewData["Title"] = "Hợp tác Quốc tế & Doanh nghiệp";
            ViewData["Description"] = "Mạng lưới đối tác quốc tế, đặc biệt hợp tác truyền thống Việt Nam - Hungary và liên kết doanh nghiệp đào tạo của VIU.";
            return View();
        }

        // GET /sinh-vien
        [HttpGet("sinh-vien")]
        public IActionResult StudentLife()
        {
            ViewData["Title"] = "Cổng Tiện ích Sinh viên & Đời sống Học đường";
            ViewData["Description"] = "Cổng thông tin sinh viên VIU: tra cứu thời khóa biểu, lịch thi, học phí, hoạt động đoàn thể, ký túc xá và việc làm.";
            return View();
        }

        // GET /api/search?q=
        [HttpGet("api/search")]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { news = new List<object>(), announcements = new List<object>(), majors = new List<object>() });
            }

            var keyword = q.Trim().ToLower();

            var newsList = await _db.News
                .Where(n => !n.IsDeleted && n.IsPublished && (n.Title.ToLower().Contains(keyword) || (n.Summary != null && n.Summary.ToLower().Contains(keyword))))
                .Take(5)
                .Select(n => new { title = n.Title, slug = n.Slug, date = n.PublishedAt != null ? n.PublishedAt.Value.ToString("dd/MM/yyyy") : "", type = "Tin tức", url = "/tin-tuc/" + n.Slug })
                .ToListAsync();

            var announceList = await _db.Announcements
                .Where(a => !a.IsDeleted && a.IsPublished && a.Title.ToLower().Contains(keyword))
                .Take(5)
                .Select(a => new { title = a.Title, slug = a.Id.ToString(), date = a.PublishedAt != null ? a.PublishedAt.Value.ToString("dd/MM/yyyy") : "", type = "Thông báo", url = "/thong-bao/" + a.Id })
                .ToListAsync();

            var majorList = await _db.Majors
                .Where(m => !m.IsDeleted && m.IsActive && (m.Name.ToLower().Contains(keyword) || m.Code.ToLower().Contains(keyword)))
                .Take(5)
                .Select(m => new { title = m.Name + " (" + m.Code + ")", slug = m.Slug, date = m.DegreeLevel ?? "Đại học", type = "Ngành học", url = "/dao-tao/nganh/" + m.Slug })
                .ToListAsync();

            return Json(new { news = newsList, announcements = announceList, majors = majorList });
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
