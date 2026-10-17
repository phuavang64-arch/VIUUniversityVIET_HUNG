using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Controllers
{
    [Route("tuyen-sinh")]
    [AllowAnonymous]
    public class AdmissionsController : Controller
    {
        private readonly IAdmissionService _admissionService;

        public AdmissionsController(IAdmissionService admissionService)
        {
            _admissionService = admissionService;
        }

        // GET: /tuyen-sinh
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var methods = await _admissionService.GetAdmissionMethodsAsync();
            var majors = await _admissionService.GetAdmissionMajorsAsync();

            var vm = new AdmissionIndexViewModel
            {
                Methods = methods,
                Majors = majors,
                NewLead = new AdmissionLead()
            };

            ViewData["Title"] = "Cổng thông tin Tuyển sinh";
            ViewData["Description"] = "Thông tin tuyển sinh đại học chính quy, các phương thức xét tuyển, bảng chỉ tiêu và đăng ký trực tuyến tại VIU.";
            return View(vm);
        }

        // GET: /tuyen-sinh/tra-cuu-diem-chuan
        // `year` used to be accepted but never used in the query, so the year dropdown on
        // the page changed nothing. It now drives the sort order of the table.
        [HttpGet("tra-cuu-diem-chuan")]
        public async Task<IActionResult> Scores(string? keyword, int? year)
        {
            // Guard against arbitrary values in the query string.
            var selectedYear = year is 2023 or 2024 or 2025 ? year.Value : 2025;

            var majors = await _admissionService.GetAdmissionMajorsAsync(keyword);

            var ordered = selectedYear switch
            {
                2023 => majors.OrderByDescending(m => m.Score2023).ThenBy(m => m.MajorName).ToList(),
                2024 => majors.OrderByDescending(m => m.Score2024).ThenBy(m => m.MajorName).ToList(),
                _ => majors.OrderByDescending(m => m.Score2025).ThenBy(m => m.MajorName).ToList()
            };

            var vm = new ScoreSearchViewModel
            {
                Keyword = keyword,
                Year = selectedYear,
                Majors = ordered
            };

            ViewData["Title"] = "Tra cứu điểm chuẩn trúng tuyển";
            ViewData["Description"] = "Tra cứu điểm chuẩn các ngành đào tạo Đại học Công nghiệp Việt - Hung qua các năm.";
            return View(vm);
        }

        // GET: /tuyen-sinh/tinh-diem
        [HttpGet("tinh-diem")]
        public async Task<IActionResult> CalculateScore()
        {
            var majors = await _admissionService.GetAdmissionMajorsAsync();
            ViewData["Title"] = "Công cụ tính điểm xét tuyển & Chọn ngành thông minh";
            ViewData["Description"] = "Nhập điểm thi THPT hoặc học bạ để hệ thống tự động tính điểm theo tổ hợp và gợi ý ngành học trúng tuyển tại VIU.";
            return View(majors);
        }

        // POST: /tuyen-sinh/dang-ky
        [HttpPost("dang-ky")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.PublicForms)]
        public async Task<IActionResult> SubmitLead([FromForm] AdmissionIndexViewModel? vm, [FromForm] AdmissionLead? newLead)
        {
            var lead = vm?.NewLead ?? newLead;

            // Validated explicitly instead of trusting ModelState: the POST is loosely bound
            // from a [FromForm] view model, so the entity's data annotations are not
            // guaranteed to have been evaluated.
            if (lead != null)
            {
                lead.FullName = lead.FullName?.Trim() ?? string.Empty;
                lead.PhoneNumber = lead.PhoneNumber?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(lead.FullName))
                    ModelState.AddModelError(nameof(lead.FullName), "Vui lòng nhập họ và tên.");

                if (string.IsNullOrWhiteSpace(lead.PhoneNumber))
                    ModelState.AddModelError(nameof(lead.PhoneNumber), "Vui lòng nhập số điện thoại.");
            }

            if (lead == null || !ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng điền đầy đủ họ tên và số điện thoại liên hệ.";
                return RedirectToAction(nameof(Index));
            }

            await _admissionService.SubmitLeadAsync(lead);
            TempData["Success"] = "Đăng ký tư vấn tuyển sinh thành công! Ban tư vấn VIU sẽ liên hệ với bạn trong thời gian sớm nhất.";
            return RedirectToAction(nameof(Index));
        }
    }
}