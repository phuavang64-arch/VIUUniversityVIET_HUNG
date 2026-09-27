using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Controllers
{
    [Route("tuyen-sinh")]
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

            ViewData["Title"] = "Cổng thông tin Tuyển sinh 2026";
            ViewData["Description"] = "Thông tin tuyển sinh đại học chính quy, các phương thức xét tuyển, bảng chỉ tiêu và đăng ký trực tuyến tại VIU.";
            return View(vm);
        }

        // GET: /tuyen-sinh/tra-cuu-diem-chuan
        [HttpGet("tra-cuu-diem-chuan")]
        public async Task<IActionResult> Scores(string? keyword, int year = 2025)
        {
            var majors = await _admissionService.GetAdmissionMajorsAsync(keyword);

            var vm = new ScoreSearchViewModel
            {
                Keyword = keyword,
                Year = year,
                Majors = majors
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
        public async Task<IActionResult> SubmitLead(AdmissionIndexViewModel vm)
        {
            if (ModelState.IsValid)
            {
                await _admissionService.SubmitLeadAsync(vm.NewLead);
                TempData["Success"] = "Đăng ký tư vấn tuyển sinh thành công! Ban tư vấn VIU sẽ liên hệ với bạn trong thời gian sớm nhất.";
                return RedirectToAction(nameof(Index));
            }

            // Re-populate if model state is invalid
            vm.Methods = await _admissionService.GetAdmissionMethodsAsync();
            vm.Majors = await _admissionService.GetAdmissionMajorsAsync();
            TempData["Error"] = "Vui lòng kiểm tra lại các thông tin bắt buộc.";
            return View(nameof(Index), vm);
        }
    }
}
