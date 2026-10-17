using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IAcademicService _academicService;
        private readonly IAdmissionService _admissionService;
        private readonly IContactService _contactService;
        private readonly ISearchService _searchService;

        public HomeController(
            IHomeService homeService,
            IAcademicService academicService,
            IAdmissionService admissionService,
            IContactService contactService,
            ISearchService searchService)
        {
            _homeService = homeService;
            _academicService = academicService;
            _admissionService = admissionService;
            _contactService = contactService;
            _searchService = searchService;
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
            ViewBag.Campuses = await _academicService.GetAllCampusesAsync();
            ViewData["Title"] = "Giới thiệu Trường Đại học Công nghiệp Việt - Hung";
            ViewData["Description"] = "Lịch sử hình thành, sứ mạng, tầm nhìn và cơ sở vật chất của Trường Đại học Công nghiệp Việt - Hung (VIU).";
            return View();
        }

        // GET /lien-he
        [HttpGet("lien-he")]
        public async Task<IActionResult> Contact()
        {
            var vm = new ContactViewModel
            {
                Campuses = await _contactService.GetCampusesAsync(),
                Message = new ContactMessage()
            };

            ViewData["Title"] = "Liên hệ";
            ViewData["Description"] = "Thông tin liên hệ các cơ sở đào tạo và biểu mẫu gửi câu hỏi tới VIU.";
            return View(vm);
        }

        // POST /lien-he
        [HttpPost("lien-he")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.PublicForms)]
        public async Task<IActionResult> SubmitContact(ContactViewModel vm)
        {
            if (ModelState.IsValid)
            {
                await _contactService.SubmitContactMessageAsync(vm.Message);
                TempData["Success"] = "Cảm ơn bạn đã gửi liên hệ! Nhà trường đã tiếp nhận và sẽ phản hồi sớm nhất qua email/SĐT.";
                return RedirectToAction(nameof(Contact));
            }

            vm.Campuses = await _contactService.GetCampusesAsync();
            TempData["Error"] = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
            return View(nameof(Contact), vm);
        }

        // GET /nghien-cuu và /hop-tac đã chuyển sang ResearchController và
        // CooperationsController: hai đường dẫn này nay là chuyên mục bài viết quản
        // lý được trong /admin, đúng như Tin tức. Nội dung giới thiệu vốn có của hai
        // trang được chuyển vào LeadSections của controller mới nên không mất gì.

        // GET /sinh-vien
        [HttpGet("sinh-vien")]
        public IActionResult StudentLife()
        {
            ViewData["Title"] = "Cổng Tiện ích Sinh viên & Đời sống Học đường";
            ViewData["Description"] = "Cổng thông tin sinh viên VIU: tra cứu thời khóa biểu, lịch thi, học phí, hoạt động đoàn thể, ký túc xá và việc làm.";
            return View();
        }

        // GET /tim-kiem?q=
        [HttpGet("tim-kiem")]
        public async Task<IActionResult> SearchPage([FromQuery] string? q)
        {
            var vm = await _searchService.SearchAsync(q, takePerType: 20);
            ViewData["Title"] = string.IsNullOrWhiteSpace(q) ? "Tìm kiếm" : $"Kết quả tìm kiếm: {q}";
            ViewData["Description"] = "Tìm kiếm tin tức, thông báo, sự kiện và ngành đào tạo tại Trường Đại học Công nghiệp Việt - Hung.";
            return View("Search", vm);
        }

        // GET /api/search?q=
        [HttpGet("api/search")]
        [EnableRateLimiting(RateLimitPolicies.Chat)]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            return Json(await _searchService.SearchApiAsync(q, takePerType: 5));
        }

        // Target of app.UseExceptionHandler("/Home/Error") outside Development.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        // Target of UseStatusCodePagesWithReExecute("/Home/NotFound") for every 404 -
        // both the ones controllers return with NotFound() and the URLs that match no
        // endpoint at all. The response status is set to 404 by the middleware before
        // the request is re-executed here, so search engines and crawlers get the
        // correct status code along with the styled page.
        //
        // ActionName is required: the method cannot be named NotFound() because
        // ControllerBase already defines NotFound() returning a NotFoundResult.
        [ActionName("NotFound")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound()
        {
            ViewData["Title"] = "Không tìm thấy trang";
            ViewData["Description"] = "Trang bạn yêu cầu không tồn tại hoặc đã được chuyển sang địa chỉ khác.";
            ViewData["NoIndex"] = true;

            // Echo back the URL that actually failed. UseStatusCodePagesWithReExecute
            // rewrites Request.Path to /Home/NotFound before this runs, so the original
            // target has to come from the request feature, which is left untouched.
            // It is rendered as text, and kept percent-encoded exactly as sent.
            var attempted = Request.HttpContext.Features
                .Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>()?.RawTarget;
            var pathOnly = attempted?.Split('?', 2)[0].Trim();

            ViewBag.AttemptedPath = string.IsNullOrWhiteSpace(pathOnly) ? null : pathOnly;

            return View();
        }
    }
}