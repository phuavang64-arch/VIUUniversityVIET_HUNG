using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services
{
    public interface IHomeService
    {
        Task<HomeViewModel> BuildHomeViewModelAsync();
    }

    /// <summary>
    /// Pulls every slice of data the homepage needs from SQL Server in
    /// parallel-ish sequential queries, keeping HomeController tiny.
    /// </summary>
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _db;
        private readonly INewsService _newsService;
        private readonly IAnnouncementService _announcementService;
        private readonly IEventService _eventService;

        public HomeService(
            ApplicationDbContext db,
            INewsService newsService,
            IAnnouncementService announcementService,
            IEventService eventService)
        {
            _db = db;
            _newsService = newsService;
            _announcementService = announcementService;
            _eventService = eventService;
        }

        public async Task<HomeViewModel> BuildHomeViewModelAsync()
        {
            var vm = new HomeViewModel
            {
                Banners = await _db.Banners.Where(b => b.IsActive && !b.IsDeleted)
                    .OrderBy(b => b.DisplayOrder).ToListAsync(),

                FeaturedMajors = await _db.Majors.Where(m => m.IsActive && !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt).Take(8).ToListAsync(),

                Partners = await _db.Partners.Where(p => p.IsActive && !p.IsDeleted)
                    .OrderBy(p => p.DisplayOrder).ToListAsync(),

                Campuses = await _db.Campuses.Where(c => c.IsActive && !c.IsDeleted)
                    .OrderBy(c => c.DisplayOrder).ToListAsync(),

                LatestNews = await _newsService.GetLatestAsync(6),
                LatestAnnouncements = await _announcementService.GetLatestAsync(6),
                UpcomingEvents = await _eventService.GetUpcomingAsync(4),

                TotalMajors = await _db.Majors.CountAsync(m => m.IsActive && !m.IsDeleted),
                TotalDepartments = await _db.Departments.CountAsync(d => d.IsActive && !d.IsDeleted),
                TotalLecturers = await _db.Lecturers.CountAsync(l => l.IsActive && !l.IsDeleted),
                // Students are seeded via Identity role membership in Phase 6; placeholder count for now.
                TotalStudents = 0
            };

            return vm;
        }
    }
}
