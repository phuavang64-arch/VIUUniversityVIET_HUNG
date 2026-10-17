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
            var bannersTask = _db.Banners.AsNoTracking()
                .Where(b => b.IsActive && !b.IsDeleted)
                .OrderBy(b => b.DisplayOrder).ToListAsync();

            var majorsTask = _db.Majors.AsNoTracking()
                .Where(m => m.IsActive && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt).Take(8).ToListAsync();

            var partnersTask = _db.Partners.AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder).ToListAsync();

            var campusesTask = _db.Campuses.AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder).ToListAsync();

            var galleryTask = _db.GalleryImages.AsNoTracking()
                .Where(g => g.IsActive && !g.IsDeleted)
                .OrderBy(g => g.DisplayOrder).Take(12).ToListAsync();

            var newsTask = _newsService.GetLatestAsync(6);
            var announcementsTask = _announcementService.GetLatestAsync(6);
            var eventsTask = _eventService.GetUpcomingAsync(4);
            var departmentsTask = _academicService.GetDepartmentsWithMajorsAsync();
            var methodsTask = _db.AdmissionMethods.AsNoTracking()
                .Where(m => !m.IsDeleted && m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var majorCountTask = _db.Majors.CountAsync(m => m.IsActive && !m.IsDeleted);
            var deptCountTask = _db.Departments.CountAsync(d => d.IsActive && !d.IsDeleted);
            var lecturerCountTask = _db.Lecturers.CountAsync(l => l.IsActive && !l.IsDeleted);

            // These are independent reads on separate connections from the pool, so running
            // them together instead of one-after-another is what actually shortens the
            // page load. Awaiting each line in sequence was the previous behaviour.
            await Task.WhenAll(
                bannersTask, majorsTask, partnersTask, campusesTask, galleryTask,
                newsTask, announcementsTask, eventsTask, departmentsTask, methodsTask,
                majorCountTask, deptCountTask, lecturerCountTask);

            var vm = new HomeViewModel
            {
                Banners = await bannersTask,
                FeaturedMajors = await majorsTask,
                Partners = await partnersTask,
                Campuses = await campusesTask,
                GalleryImages = await galleryTask,
                LatestNews = await newsTask,
                LatestAnnouncements = await announcementsTask,
                UpcomingEvents = await eventsTask,
                Departments = await departmentsTask,
                AdmissionMethods = await methodsTask,
                TotalMajors = await majorCountTask,
                TotalDepartments = await deptCountTask,
                TotalLecturers = await lecturerCountTask,

                // Student records are introduced with the Student Portal phase. Until then
                // this reads the editable setting rather than being hardcoded to 0, which
                // was silently publishing a wrong number on the homepage.
                TotalStudents = ParseCount(await _settings.GetAsync("stats.studentTotal", "0"))
            };

            return vm;
        }

        private static int ParseCount(string value) =>
            int.TryParse(value, out var n) && n >= 0 ? n : 0;
    }
}