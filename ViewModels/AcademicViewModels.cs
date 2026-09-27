using VIUUniversity.Models;

namespace VIUUniversity.ViewModels
{
    public class AdmissionIndexViewModel
    {
        public List<AdmissionMethod> Methods { get; set; } = new();
        public List<AdmissionMajor> Majors { get; set; } = new();
        public AdmissionLead NewLead { get; set; } = new();
    }

    public class ScoreSearchViewModel
    {
        public string? Keyword { get; set; }
        public int Year { get; set; } = 2025;
        public List<AdmissionMajor> Majors { get; set; } = new();
    }

    public class AcademicIndexViewModel
    {
        public List<Department> Departments { get; set; } = new();
        public List<Major> Majors { get; set; } = new();
    }

    public class ContactViewModel
    {
        public ContactMessage Message { get; set; } = new();
        public List<Campus> Campuses { get; set; } = new();
    }
}
