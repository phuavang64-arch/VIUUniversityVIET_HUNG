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
                TotalDepartments = await _db.Departments.CountAsync(d => !d.IsDeleted),
                TotalMajors = await _db.Majors.CountAsync(m => !m.IsDeleted),
                TotalLecturers = await _db.Lecturers.CountAsync(l => !l.IsDeleted),
                TotalNews = await _db.News.CountAsync(n => !n.IsDeleted),
                TotalAnnouncements = await _db.Announcements.CountAsync(a => !a.IsDeleted),
                TotalEvents = await _db.Events.CountAsync(e => !e.IsDeleted),

                NewsByMonth = await _db.News
                    .Where(n => !n.IsDeleted && n.PublishedAt != null)
                    .GroupBy(n => new { n.PublishedAt!.Value.Year, n.PublishedAt.Value.Month })
                    .Select(g => new MonthlyCount
                    {
                        Label = g.Key.Month + "/" + g.Key.Year,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}
