using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/lien-he")]
    [Authorize(Roles = "Admin,Editor")]
    public class ContactMessagesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ContactMessagesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET admin/lien-he
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1, string? keyword = null, bool? isRead = null)
        {
            var query = _db.ContactMessages
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(m =>
                    m.FullName.ToLower().Contains(kw) ||
                    m.Email.ToLower().Contains(kw) ||
                    (m.PhoneNumber != null && m.PhoneNumber.Contains(kw)) ||
                    m.Subject.ToLower().Contains(kw));
            }

            if (isRead.HasValue)
            {
                query = query.Where(m => m.IsRead == isRead.Value);
            }

            var total = await query.CountAsync();
            var pageSize = 15;
            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.IsRead = isRead;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;
            ViewBag.TotalUnread = await _db.ContactMessages.CountAsync(m => !m.IsDeleted && !m.IsRead);

            return View(items);
        }

        // GET admin/lien-he/chi-tiet/{id}
        [HttpGet("chi-tiet/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null || msg.IsDeleted) return NotFound();

            if (!msg.IsRead)
            {
                msg.IsRead = true;
                msg.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return View(msg);
        }

        // POST admin/lien-he/toggle-read/{id}
        [HttpPost("toggle-read/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRead(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null || msg.IsDeleted) return NotFound();

            msg.IsRead = !msg.IsRead;
            msg.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = msg.IsRead
                ? $"Đã đánh dấu đã đọc thư từ {msg.FullName}."
                : $"Đã chuyển thư từ {msg.FullName} về chưa đọc.";

            return RedirectToAction(nameof(Index));
        }

        // POST admin/lien-he/delete/{id}
        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            msg.IsDeleted = true;
            msg.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa tin nhắn liên hệ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
