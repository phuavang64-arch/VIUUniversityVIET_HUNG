using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _db;

        public ContactService(ApplicationDbContext db) => _db = db;

        public async Task SubmitContactMessageAsync(ContactMessage message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.IsActive = true;
            message.IsDeleted = false;

            _db.ContactMessages.Add(message);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Partner>> GetPartnersAsync()
        {
            return await _db.Partners
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<Campus>> GetCampusesAsync()
        {
            return await _db.Campuses
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }
    }
}