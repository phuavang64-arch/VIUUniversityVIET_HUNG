using VIUUniversity.Models;

namespace VIUUniversity.Services.Interfaces
{
    public interface IAcademicService
    {
        Task<List<Department>> GetDepartmentsWithMajorsAsync();
        Task<Department?> GetDepartmentBySlugAsync(string slug);
        Task<List<Major>> GetAllMajorsAsync();
        Task<Major?> GetMajorBySlugAsync(string slug);
        Task<List<Lecturer>> GetAllLecturersAsync(int? departmentId = null);
        Task<List<Campus>> GetAllCampusesAsync();
    }
}
