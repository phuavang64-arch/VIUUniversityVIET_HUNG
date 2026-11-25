using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services
{
    /// <summary>
    /// Assembles everything the homepage renders, so HomeController.Index() stays a thin
    /// "call service, return view" method.
    /// </summary>
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _db;
        private readonly INewsService _newsService;
        private readonly IAnnouncementService _announcementService;
        private readonly IEventService _eventService;
        private readonly IAcademicService _academicService;
        private readonly ISiteSettingService _settings;

        public HomeService(
            ApplicationDbContext db,
            INewsService newsService,
            IAnnouncementService announcementService,
            IEventService eventService,
            IAcademicService academicService,
            ISiteSettingService settings)
        {
            _db = db;
            _newsService = newsService;
            _announcementService = announcementService;
            _eventService = eventService;
            _academicService = academicService;
            _settings = settings;
        }

        public async Task<HomeViewModel> BuildHomeViewModelAsync()
        {
            // DbContext is not thread-safe, so these reads are awaited one at a time.
            // (Running them concurrently on the same context used to throw
            // "A second operation was started..." with the MySQL provider.)
            var banners = await _db.Banners.AsNoTracking()
                .Where(b => b.IsActive && !b.IsDeleted)
                .OrderBy(b => b.DisplayOrder).ToListAsync();

            var majors = await _db.Majors.AsNoTracking()
                .Where(m => m.IsActive && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt).Take(8).ToListAsync();

            var partners = await _db.Partners.AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder).ToListAsync();

            var campuses = await _db.Campuses.AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder).ToListAsync();

            var gallery = await _db.GalleryImages.AsNoTracking()
                .Where(g => g.IsActive && !g.IsDeleted)
                .OrderBy(g => g.DisplayOrder).Take(12).ToListAsync();

            var news = await _newsService.GetLatestAsync(6);
            var announcements = await _announcementService.GetLatestAsync(6);
            var events = await _eventService.GetUpcomingAsync(4);
            var departments = await _academicService.GetDepartmentsWithMajorsAsync();
            var methods = await _db.AdmissionMethods.AsNoTracking()
                .Where(m => !m.IsDeleted && m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var majorCount = await _db.Majors.CountAsync(m => m.IsActive && !m.IsDeleted);
            var deptCount = await _db.Departments.CountAsync(d => d.IsActive && !d.IsDeleted);
            var lecturerCount = await _db.Lecturers.CountAsync(l => l.IsActive && !l.IsDeleted);

            var vm = new HomeViewModel
            {
                Banners = banners,
                FeaturedMajors = majors,
                Partners = partners,
                Campuses = campuses,
                GalleryImages = gallery,
                LatestNews = news,
                LatestAnnouncements = announcements,
                UpcomingEvents = events,
                Departments = departments,
                AdmissionMethods = methods,
                TotalMajors = majorCount,
                TotalDepartments = deptCount,
                TotalLecturers = lecturerCount,

                TotalStudents = ParseCount(await _settings.GetAsync("stats.studentTotal", "0"))
            };

            return vm;
        }

        private static int ParseCount(string value) =>
            int.TryParse(value, out var n) && n >= 0 ? n : 0;
    }
}