using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/thi-sinh")]
    [Authorize(Roles = "Admin,Editor")]
    public class AdmissionLeadsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdmissionLeadsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET admin/thi-sinh
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null, bool? processed = null)
        {
            var query = _db.AdmissionLeads
                .Where(l => !l.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(l =>
                    l.FullName.ToLower().Contains(kw) ||
                    l.PhoneNumber.Contains(kw) ||
                    (l.Email != null && l.Email.ToLower().Contains(kw)) ||
                    (l.DesiredMajor != null && l.DesiredMajor.ToLower().Contains(kw)));
            }

            if (processed.HasValue)
                query = query.Where(l => l.IsProcessed == processed.Value);

            var total = await query.CountAsync();
            var pageSize = 15;
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Processed = processed;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;
            ViewBag.TotalPending = await _db.AdmissionLeads.CountAsync(l => !l.IsDeleted && !l.IsProcessed);
            return View(items);
        }

        // GET admin/thi-sinh/{id}
        [HttpGet("chi-tiet/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var lead = await _db.AdmissionLeads.FindAsync(id);
            if (lead == null || lead.IsDeleted) return NotFound();
            return View(lead);
        }

        // POST admin/thi-sinh/toggle-processed/{id}
        [HttpPost("toggle-processed/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProcessed(int id)
        {
            var lead = await _db.AdmissionLeads.FindAsync(id);
            if (lead == null || lead.IsDeleted) return NotFound();
            lead.IsProcessed = !lead.IsProcessed;
            lead.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = lead.IsProcessed
                ? $"Đã đánh dấu thí sinh {lead.FullName} là đã xử lý."
                : $"Đã chuyển thí sinh {lead.FullName} về chưa xử lý.";
            return RedirectToAction(nameof(Index));
        }

        // POST admin/thi-sinh/delete/{id}
        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _db.AdmissionLeads.FindAsync(id);
            if (lead == null) return NotFound();
            lead.IsDeleted = true;
            lead.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa hồ sơ thí sinh.";
            return RedirectToAction(nameof(Index));
        }

        // POST admin/thi-sinh/delete-all-processed
        [HttpPost("delete-all-processed")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAllProcessed()
        {
            var leads = await _db.AdmissionLeads
                .Where(l => !l.IsDeleted && l.IsProcessed)
                .ToListAsync();
            foreach (var l in leads)
            {
                l.IsDeleted = true;
                l.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa {leads.Count} hồ sơ đã xử lý.";
            return RedirectToAction(nameof(Index));
        }
    }
}
