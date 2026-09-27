using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _db;
        public EventService(ApplicationDbContext db) => _db = db;

        public async Task<PagedResult<Event>> GetPublishedAsync(int page, int pageSize)
        {
            var query = _db.Events.Where(e => e.IsPublished && !e.IsDeleted);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(e => e.StartAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Event> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public Task<List<Event>> GetUpcomingAsync(int count) =>
            _db.Events.Where(e => e.IsPublished && !e.IsDeleted && e.StartAt >= DateTime.UtcNow)
                .OrderBy(e => e.StartAt).Take(count).ToListAsync();

        public Task<Event?> GetBySlugAsync(string slug) =>
            _db.Events.FirstOrDefaultAsync(e => e.Slug == slug && e.IsPublished && !e.IsDeleted);

        public async Task<PagedResult<Event>> GetAllForAdminAsync(int page, int pageSize)
        {
            var query = _db.Events.Where(e => !e.IsDeleted);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Event> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public Task<Event?> GetByIdAsync(int id) => _db.Events.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        public async Task<Event> CreateAsync(Event item)
        {
            item.CreatedAt = DateTime.UtcNow;
            _db.Events.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(Event item)
        {
            item.UpdatedAt = DateTime.UtcNow;
            _db.Events.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.Events.FindAsync(id);
            if (entity == null) return;
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
