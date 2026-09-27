using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly ApplicationDbContext _db;
        public AnnouncementService(ApplicationDbContext db) => _db = db;

        public async Task<PagedResult<Announcement>> GetPublishedAsync(int page, int pageSize)
        {
            var query = _db.Announcements.Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.IsImportant)
                .ThenByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new PagedResult<Announcement> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public Task<List<Announcement>> GetLatestAsync(int count) =>
            _db.Announcements.Where(a => a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.IsImportant).ThenByDescending(a => a.PublishedAt)
                .Take(count).ToListAsync();

        public Task<Announcement?> GetByIdAsync(int id) =>
            _db.Announcements.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        public async Task<PagedResult<Announcement>> GetAllForAdminAsync(int page, int pageSize)
        {
            var query = _db.Announcements.Include(a => a.Category).Where(a => !a.IsDeleted);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Announcement> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public async Task<Announcement> CreateAsync(Announcement item)
        {
            item.CreatedAt = DateTime.UtcNow;
            _db.Announcements.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(Announcement item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            _db.Announcements.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Announcements.FindAsync(id);
            if (entity == null) return;
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
