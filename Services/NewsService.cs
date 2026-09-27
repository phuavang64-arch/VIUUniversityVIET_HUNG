using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services
{
    public class NewsService : INewsService
    {
        private readonly ApplicationDbContext _db;

        public NewsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<News>> GetPublishedAsync(int page, int pageSize, string? keyword = null, int? categoryId = null)
        {
            var query = _db.News
                .Include(n => n.Category)
                .Where(n => n.IsPublished && !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(n => n.Title.Contains(keyword) || (n.Summary != null && n.Summary.Contains(keyword)));

            if (categoryId.HasValue)
                query = query.Where(n => n.CategoryId == categoryId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<News> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public async Task<News?> GetBySlugAsync(string slug)
        {
            var news = await _db.News
                .Include(n => n.Category)
                .Include(n => n.Author)
                .FirstOrDefaultAsync(n => n.Slug == slug && n.IsPublished && !n.IsDeleted);

            if (news != null)
            {
                // Increment view count without loading a second tracked copy.
                news.ViewCount++;
                await _db.SaveChangesAsync();
            }

            return news;
        }

        public async Task<List<News>> GetLatestAsync(int count)
        {
            return await _db.News
                .Include(n => n.Category)
                .Where(n => n.IsPublished && !n.IsDeleted)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<PagedResult<News>> GetAllForAdminAsync(int page, int pageSize, string? keyword = null)
        {
            var query = _db.News.Include(n => n.Category).Where(n => !n.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(n => n.Title.Contains(keyword));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<News> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public Task<News?> GetByIdAsync(int id) =>
            _db.News.Include(n => n.Category).FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

        public async Task<News> CreateAsync(News news)
        {
            news.CreatedAt = DateTime.UtcNow;
            _db.News.Add(news);
            await _db.SaveChangesAsync();
            return news;
        }

        public async Task UpdateAsync(News news)
        {
            news.UpdatedAt = DateTime.UtcNow;
            _db.News.Update(news);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.News.FindAsync(id);
            if (entity == null) return;
            entity.IsDeleted = true; // soft delete keeps history/audit intact
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task SetPublishStateAsync(int id, bool isPublished)
        {
            var entity = await _db.News.FindAsync(id);
            if (entity == null) return;
            entity.IsPublished = isPublished;
            entity.PublishedAt = isPublished ? (entity.PublishedAt ?? DateTime.UtcNow) : entity.PublishedAt;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
