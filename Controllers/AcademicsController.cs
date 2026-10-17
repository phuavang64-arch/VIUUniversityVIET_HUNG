using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Controllers
{
    [Route("dao-tao")]
    [AllowAnonymous]
    public class AcademicsController : Controller
    {
        private readonly IAcademicService _academicService;
        private readonly IGalleryService _galleryService;

        public AcademicsController(IAcademicService academicService, IGalleryService galleryService)
        {
            _academicService = academicService;
            _galleryService = galleryService;
        }

        // GET: /dao-tao
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var departments = await _academicService.GetDepartmentsWithMajorsAsync();
            var majors = await _academicService.GetAllMajorsAsync();

            var vm = new AcademicIndexViewModel
            {
                Departments = departments,
                Majors = majors
            };

            ViewData["Title"] = "Chương trình & Khoa Đào tạo";
            ViewData["Description"] = "Tổng quan các ngành đào tạo đại học, các khoa chuyên môn tại Trường Đại học Công nghiệp Việt - Hung.";
            return View(vm);
        }

        // GET: /dao-tao/khoa/{slug}
        [HttpGet("khoa/{slug}")]
        public async Task<IActionResult> DepartmentDetail(string slug)
        {
            var department = await _academicService.GetDepartmentBySlugAsync(slug);
            if (department == null)
            {
                return NotFound();
            }

            // Ảnh minh hoạ cho khoa: ảnh được gắn nhãn theo tên khoa trong thư viện.
            var imageKeywords = new[] { "cong-nghe-thong-tin", "dien-dien-tu", "kinhte", "kinh-te" };
            var allImages = await _galleryService.GetImagesAsync();

            ViewBag.DepartmentGallery = allImages
                .Where(g => imageKeywords.Any(k =>
                    g.ImageUrl.Contains(k, StringComparison.OrdinalIgnoreCase)
                    || (g.Caption ?? string.Empty).Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .ToList();

            ViewData["Title"] = department.Name;
            ViewData["Description"] = department.Description ?? $"Thông tin Khoa {department.Name} tại VIU.";
            return View(department);
        }

        // GET: /dao-tao/nganh/{slug}
        [HttpGet("nganh/{slug}")]
        public async Task<IActionResult> MajorDetail(string slug)
        {
            var major = await _academicService.GetMajorBySlugAsync(slug);
            if (major == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Ngành {major.Name}";
            ViewData["Description"] = major.Description ?? $"Chương trình đào tạo ngành {major.Name} tại VIU.";
            return View(major);
        }

        // Support direct /dao-tao/{slug} link from homepage
        [HttpGet("{slug}")]
        public async Task<IActionResult> DetailBySlug(string slug)
        {
            if (slug == "giang-vien")
            {
                return await Lecturers(null);
            }

            var major = await _academicService.GetMajorBySlugAsync(slug);
            if (major != null)
            {
                ViewData["Title"] = $"Ngành {major.Name}";
                ViewData["Description"] = major.Description ?? $"Chương trình đào tạo ngành {major.Name} tại VIU.";
                return View("MajorDetail", major);
            }

            var dept = await _academicService.GetDepartmentBySlugAsync(slug);
            if (dept != null)
            {
                ViewData["Title"] = dept.Name;
                ViewData["Description"] = dept.Description ?? $"Thông tin Khoa {dept.Name} tại VIU.";
                return View("DepartmentDetail", dept);
            }

            return NotFound();
        }

        // GET: /dao-tao/giang-vien
        [HttpGet("giang-vien")]
        public async Task<IActionResult> Lecturers(int? departmentId)
        {
            var lecturers = await _academicService.GetAllLecturersAsync(departmentId);
            var departments = await _academicService.GetDepartmentsWithMajorsAsync();

            ViewBag.Departments = departments;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewData["Title"] = "Đội ngũ Giảng viên";
            ViewData["Description"] = "Đội ngũ giảng viên, nhà khoa học tâm huyết, giàu kinh nghiệm của VIU.";
            return View("Lecturers", lecturers);
        }
    }
}
