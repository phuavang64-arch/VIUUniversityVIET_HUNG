using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    public class AdmissionService : IAdmissionService
    {
        private readonly ApplicationDbContext _db;

        public AdmissionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<AdmissionMethod>> GetAdmissionMethodsAsync()
        {
            return await _db.AdmissionMethods
                .Where(m => !m.IsDeleted && m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<AdmissionMajor>> GetAdmissionMajorsAsync(string? keyword = null)
        {
            var query = _db.AdmissionMajors
                .Where(m => !m.IsDeleted && m.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(m =>
                    m.MajorName.ToLower().Contains(keyword) ||
                    m.MajorCode.ToLower().Contains(keyword) ||
                    m.SubjectCombinations.ToLower().Contains(keyword));
            }

            return await query.OrderBy(m => m.MajorName).ToListAsync();
        }

        public async Task<bool> SubmitLeadAsync(AdmissionLead lead)
        {
            lead.CreatedAt = DateTime.UtcNow;
            lead.IsActive = true;
            lead.IsDeleted = false;
            _db.AdmissionLeads.Add(lead);
            var result = await _db.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SubmitContactMessageAsync(ContactMessage message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsActive = true;
            message.IsDeleted = false;
            _db.ContactMessages.Add(message);
            var result = await _db.SaveChangesAsync();
            return result > 0;
        }
    }
}
