using VIUUniversity.Models;

namespace VIUUniversity.ViewModels
{
    /// <summary>
    /// Everything the homepage needs, assembled by HomeService from the
    /// database in one place so HomeController.Index() stays a thin
    /// "call service, return view" method.
    /// </summary>
    public class HomeViewModel
    {
        public List<Banner> Banners { get; set; } = new();
        public List<Major> FeaturedMajors { get; set; } = new();
        public List<News> LatestNews { get; set; } = new();
        public List<Announcement> LatestAnnouncements { get; set; } = new();
        public List<Event> UpcomingEvents { get; set; } = new();
        public List<Partner> Partners { get; set; } = new();
        public List<Campus> Campuses { get; set; } = new();
        public List<GalleryImage> GalleryImages { get; set; } = new();

        /// <summary>Khoa kèm ngành đào tạo, dùng cho khu vực "Các khoa" trên trang chủ.</summary>
        public List<Department> Departments { get; set; } = new();

        /// <summary>Phương thức xét tuyển lấy từ cơ sở dữ liệu.</summary>
        public List<AdmissionMethod> AdmissionMethods { get; set; } = new();

        // Statistics strip
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalMajors { get; set; }
        public int TotalDepartments { get; set; }
    }
}
