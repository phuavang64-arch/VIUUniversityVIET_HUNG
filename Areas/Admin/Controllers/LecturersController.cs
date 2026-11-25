using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    /// <summary>
    /// Quản trị đội ngũ giảng viên.
    ///
    /// Giảng viên thuộc một khoa (Department). Mỗi giảng viên có ảnh đại diện,
    /// học vị, email, điện thoại và tiểu sử. Ảnh tải lên qua ImageUploadHelper
    /// (magic bytes + giới hạn 5 MB) để tránh file HTML/SVG đổi tên .jpg.
    /// </summary>
    [Area("Admin")]
    [Route("admin/giang-vien")]
    [Authorize(Roles = "Admin,Editor")]
    public class LecturersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public LecturersController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET admin/giang-vien
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null, int? departmentId = null)
        {
            var query = _db.Lecturers
                .Include(l => l.Department)
                .Where(l => !l.IsDeleted)
                .AsQueryable();

            if (departmentId.HasValue)
            {
                query = query.Where(l => l.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(l =>
                    l.FullName.ToLower().Contains(kw) ||
                    (l.AcademicTitle != null && l.AcademicTitle.ToLower().Contains(kw)) ||
                    (l.Email != null && l.Email.ToLower().Contains(kw)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(l => l.DepartmentId)
                .ThenBy(l => l.FullName)
                .Skip((page < 1 ? 0 : (page - 1) * 10))
                .Take(10)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
            ViewBag.TotalCount = total;
            ViewBag.PageNumber = page < 1 ? 1 : page;
            return View(items);
        }

        // GET admin/giang-vien/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
            return View(new Lecturer { AcademicTitle = "ThS." });
        }

        // POST admin/giang-vien/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lecturer model, IFormFile? avatarFile)
        {
            ModelState.Remove(nameof(Lecturer.Department));
            ModelState.Remove(nameof(Lecturer.User));

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
                return View(model);
            }

            if (avatarFile is { Length: > 0 })
            {
                try
                {
                    model.Avatar = await ImageUploadHelper.SaveAsync(avatarFile, _env.WebRootPath, "lecturers");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Ảnh đại diện: " + ex.Message);
                    ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
                    return View(model);
                }
            }

            model.CreatedAt = DateTime.UtcNow;
            _db.Lecturers.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm giảng viên {model.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        // GET admin/giang-vien/edit/{id}
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var lecturer = await _db.Lecturers
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (lecturer == null) return NotFound();

            ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
            return View(lecturer);
        }

        // POST admin/giang-vien/edit/{id}
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lecturer model, IFormFile? avatarFile)
        {
            if (id != model.Id) return BadRequest();

            ModelState.Remove(nameof(Lecturer.Department));
            ModelState.Remove(nameof(Lecturer.User));

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
                return View(model);
            }

            var existing = await _db.Lecturers.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (existing == null) return NotFound();

            existing.FullName = model.FullName;
            existing.AcademicTitle = model.AcademicTitle;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Biography = model.Biography;
            existing.DepartmentId = model.DepartmentId;
            existing.UpdatedAt = DateTime.UtcNow;

            if (avatarFile is { Length: > 0 })
            {
                try
                {
                    existing.Avatar = await ImageUploadHelper.SaveAsync(avatarFile, _env.WebRootPath, "lecturers");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Ảnh đại diện: " + ex.Message);
                    ViewBag.Departments = await _db.Departments.Where(d => !d.IsDeleted).ToListAsync();
                    return View(model);
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật giảng viên {existing.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        // POST admin/giang-vien/delete/{id}
        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lecturer = await _db.Lecturers.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
            if (lecturer == null) return NotFound();

            lecturer.IsDeleted = true;
            lecturer.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xoá giảng viên {lecturer.FullName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}