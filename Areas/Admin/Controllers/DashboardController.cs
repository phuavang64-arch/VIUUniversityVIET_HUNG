using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.ViewModels.Admin;

namespace VIUUniversity.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalDepartments     = await _db.Departments.CountAsync(d => !d.IsDeleted),
                TotalMajors          = await _db.Majors.CountAsync(m => !m.IsDeleted),
                TotalLecturers       = await _db.Lecturers.CountAsync(l => !l.IsDeleted),
                TotalNews            = await _db.News.CountAsync(n => !n.IsDeleted),
                TotalAnnouncements   = await _db.Announcements.CountAsync(a => !a.IsDeleted),
                TotalEvents          = await _db.Events.CountAsync(e => !e.IsDeleted),
                TotalAdmissionLeads  = await _db.AdmissionLeads.CountAsync(l => !l.IsDeleted),
                TotalPendingLeads    = await _db.AdmissionLeads.CountAsync(l => !l.IsDeleted && !l.IsProcessed),
                TotalGalleryImages   = await _db.GalleryImages.CountAsync(g => !g.IsDeleted),
                TotalContactMessages = await _db.ContactMessages.CountAsync(c => !c.IsDeleted),
                TotalUnreadMessages  = await _db.ContactMessages.CountAsync(c => !c.IsDeleted && !c.IsRead),

                NewsByMonth = (await _db.News
                    .Where(n => !n.IsDeleted && n.PublishedAt != null)
                    .Select(n => new { n.PublishedAt!.Value.Year, n.PublishedAt.Value.Month })
                    .ToListAsync())
                    .GroupBy(n => new { n.Year, n.Month })
                    .Select(g => new MonthlyCount
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Label = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count()
                    })
                    // Sort on (Year, Month); ordering by the "M/YYYY" label string would
                    // place 10/2026 before 2/2026.
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Take(12)
                    .ToList(),

                LeadsByMonth = (await _db.AdmissionLeads
                    .Where(l => !l.IsDeleted)
                    .Select(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                    .ToListAsync())
                    .GroupBy(l => new { l.Year, l.Month })
                    .Select(g => new MonthlyCount
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Label = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Take(6)
                    .ToList()
            };

            return View(vm);
        }
    }
}
