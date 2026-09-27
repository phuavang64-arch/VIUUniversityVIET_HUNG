using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    public class AcademicService : IAcademicService
    {
        private readonly ApplicationDbContext _db;

        public AcademicService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Department>> GetDepartmentsWithMajorsAsync()
        {
            return await _db.Departments
                .Include(d => d.Majors.Where(m => !m.IsDeleted && m.IsActive))
                .Include(d => d.Lecturers.Where(l => !l.IsDeleted && l.IsActive))
                .Where(d => !d.IsDeleted && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<Department?> GetDepartmentBySlugAsync(string slug)
        {
            return await _db.Departments
                .Include(d => d.Majors.Where(m => !m.IsDeleted && m.IsActive))
                .Include(d => d.Lecturers.Where(l => !l.IsDeleted && l.IsActive))
                .FirstOrDefaultAsync(d => d.Slug == slug && !d.IsDeleted && d.IsActive);
        }

        public async Task<List<Major>> GetAllMajorsAsync()
        {
            return await _db.Majors
                .Include(m => m.Department)
                .Include(m => m.Subjects.Where(s => !s.IsDeleted && s.IsActive))
                .Where(m => !m.IsDeleted && m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<Major?> GetMajorBySlugAsync(string slug)
        {
            return await _db.Majors
                .Include(m => m.Department)
                .Include(m => m.Subjects.Where(s => !s.IsDeleted && s.IsActive))
                .FirstOrDefaultAsync(m => m.Slug == slug && !m.IsDeleted && m.IsActive);
        }

        public async Task<List<Lecturer>> GetAllLecturersAsync(int? departmentId = null)
        {
            var query = _db.Lecturers
                .Include(l => l.Department)
                .Where(l => !l.IsDeleted && l.IsActive);

            if (departmentId.HasValue)
            {
                query = query.Where(l => l.DepartmentId == departmentId.Value);
            }

            return await query.OrderBy(l => l.FullName).ToListAsync();
        }

        public async Task<List<Campus>> GetAllCampusesAsync()
        {
            return await _db.Campuses
                .Where(c => !c.IsDeleted && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }
    }
}
