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
        private readonly IContentSanitizer _sanitizer;

        public NewsService(ApplicationDbContext db, IContentSanitizer sanitizer)
        {
            _db = db;
            _sanitizer = sanitizer;
        }

        public async Task<PagedResult<News>> GetPublishedAsync(int page, int pageSize, string? keyword = null, int? categoryId = null)
        {
            var query = _db.News
                .Include(n => n.Category)
                .Where(n => n.IsPublished && !n.IsDeleted);

            // Lower-case both sides so the result matches HomeController's search, which
            // already normalises the keyword. SQLite's Contains/== is case sensitive.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(kw) ||
                    (n.Summary != null && n.Summary.ToLower().Contains(kw)));
            }

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
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == slug && n.IsPublished && !n.IsDeleted);

            if (news == null)
                return null;

            // Increment with a single atomic UPDATE. The previous implementation loaded the
            // entity tracked, did news.ViewCount++ and saved, which rewrote every column
            // (clobbering concurrent admin edits) and silently lost updates when two
            // visitors opened the same article at once.
            await IncrementViewCountAsync(news.Id);

            news.ViewCount++;
            return news;
        }

        /// <summary>
        /// Tăng lượt đọc mà không nạp entity vào change tracker — ExecuteUpdate chỉ
        /// ghi đúng cột ViewCount nên không đè lên những lần sửa bài đang chạy ở
        /// /admin, và là thao tác nguyên tử (hai người mở cùng bài không ghi đè nhau).
        /// </summary>
        private Task IncrementViewCountAsync(int newsId) =>
            _db.News
                .Where(n => n.Id == newsId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ViewCount, n => n.ViewCount + 1));

        public async Task<List<News>> GetLatestAsync(int count)
        {
            return await _db.News
                .Include(n => n.Category)
                .Where(n => n.IsPublished && !n.IsDeleted)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<News>> GetRelatedAsync(int newsId, int? categoryId, int count = 3)
        {
            // Bài không có chuyên mục (CategoryId null) không lọc theo danh mục:
            // "n.CategoryId == null" sẽ không khớp với SQL NULL nên ta bỏ hẳn điều
            // kiện, còn vòng lấp bù bên dưới sẽ lấp đủ số bài còn thiếu.
            var scoped = _db.News
                .Include(n => n.Category)
                .Where(n => n.IsPublished && !n.IsDeleted && n.Id != newsId)
                .AsQueryable();

            if (categoryId.HasValue)
                scoped = scoped.Where(n => n.CategoryId == categoryId.Value);

            var related = await scoped
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .ToListAsync();

            if (related.Count < count)
            {
                var extra = await _db.News
                    .Include(n => n.Category)
                    .Where(n => n.IsPublished && !n.IsDeleted && n.Id != newsId && !related.Select(r => r.Id).Contains(n.Id))
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(count - related.Count)
                    .ToListAsync();
                related.AddRange(extra);
            }

            return related;
        }

        public async Task<PagedResult<News>> GetAllForAdminAsync(int page, int pageSize, string? keyword = null)
        {
            var query = _db.News.Include(n => n.Category).Where(n => !n.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<News> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        // ================= Theo chuyên mục =================

        public async Task<PagedResult<News>> GetPublishedBySectionAsync(
            string section, int page, int pageSize, string? keyword = null)
        {
            var sec = ContentSection.Normalise(section);

            var query = _db.News
                .Include(n => n.Category)
                .Where(n => n.Section == sec && n.IsPublished && !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(kw) ||
                    (n.Summary != null && n.Summary.ToLower().Contains(kw)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<News> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public async Task<News?> GetBySlugInSectionAsync(string slug, string section)
        {
            var sec = ContentSection.Normalise(section);

            var news = await _db.News
                .Include(n => n.Category)
                .Include(n => n.Author)
                .AsNoTracking()
                .FirstOrDefaultAsync(n =>
                    n.Slug == slug && n.Section == sec && n.IsPublished && !n.IsDeleted);

            if (news == null)
                return null;

            await IncrementViewCountAsync(news.Id);
            news.ViewCount++;
            return news;
        }

        public async Task<List<News>> GetRelatedInSectionAsync(string section, int newsId, int count = 3)
        {
            var sec = ContentSection.Normalise(section);

            var related = await _db.News
                .Include(n => n.Category)
                .Where(n => n.Section == sec && n.IsPublished && !n.IsDeleted && n.Id != newsId)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .ToListAsync();

            return related;
        }

        public Task<int> CountPublishedBySectionAsync(string section)
        {
            var sec = ContentSection.Normalise(section);
            return _db.News.CountAsync(n =>
                n.Section == sec && n.IsPublished && !n.IsDeleted);
        }

        public async Task<PagedResult<News>> GetAllForAdminBySectionAsync(
            string section, int page, int pageSize, string? keyword = null)
        {
            var sec = ContentSection.Normalise(section);
            var query = _db.News
                .Include(n => n.Category)
                .Where(n => n.Section == sec && !n.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<News> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
        }

        public Task<News?> GetByIdAsync(int id) =>
            _db.News.Include(n => n.Category).AsNoTracking().FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

        public async Task<News> CreateAsync(News news)
        {
            SanitizeForStorage(news);

            news.CreatedAt = DateTime.UtcNow;
            news.Slug = await EnsureUniqueSlugAsync(news.Slug);
            news.Section = ContentSection.Normalise(news.Section);

            if (news.IsPublished && news.PublishedAt == null)
                news.PublishedAt = DateTime.UtcNow;

            _db.News.Add(news);
            await _db.SaveChangesAsync();
            return news;
        }

        public async Task UpdateAsync(News news)
        {
            SanitizeForStorage(news);

            var existing = await _db.News.FirstOrDefaultAsync(n => n.Id == news.Id);
            if (existing == null)
                return;

            existing.Title = news.Title;
            existing.Slug = await EnsureUniqueSlugAsync(news.Slug, news.Id);
            existing.Summary = news.Summary;
            existing.Content = news.Content;
            existing.Thumbnail = news.Thumbnail;
            existing.CategoryId = news.CategoryId;
            existing.IsPublished = news.IsPublished;
            existing.AuthorId = news.AuthorId;
            existing.Section = ContentSection.Normalise(news.Section);

            // Trường riêng của chuyên mục Học phí.
            existing.DocumentNumber = news.DocumentNumber;
            existing.IssuedDate = news.IssuedDate;
            existing.AcademicYear = news.AcademicYear;
            existing.AttachmentUrl = news.AttachmentUrl;
            existing.AttachmentName = news.AttachmentName;
            existing.VideoUrl = news.VideoUrl;

            if (news.IsPublished && existing.PublishedAt == null)
                existing.PublishedAt = news.PublishedAt ?? DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            // Only tracked columns above are written, so concurrent view-count updates
            // (which bypass the change tracker) are never overwritten.
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

        public Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Task.FromResult(false);

            var normalised = slug.Trim().ToLower();
            return _db.News.AnyAsync(n => n.Slug.ToLower() == normalised
                                          && (excludeId == null || n.Id != excludeId.Value));
        }

        public async Task<string> EnsureUniqueSlugAsync(string slug, int? excludeId = null)
        {
            var baseSlug = string.IsNullOrWhiteSpace(slug)
                ? "bai-viet"
                : slug.Trim().ToLower();

            if (!await SlugExistsAsync(baseSlug, excludeId))
                return baseSlug;

            for (var suffix = 2; suffix < 200; suffix++)
            {
                var candidate = $"{baseSlug}-{suffix}";
                if (!await SlugExistsAsync(candidate, excludeId))
                    return candidate;
            }

            // Practically unreachable; guarantees a usable value regardless.
            return $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(baseSlug.Length + 9, 260)];
        }

        /// <summary>
        /// Content is stored as rich HTML and rendered with @Html.Raw, so it must be
        /// stripped of scripts and event handlers before it ever reaches the database.
        ///
        /// Title and Summary are plain text: Razor already HTML-encodes them on output,
        /// so they are only stripped of stray markup here. Encoding them as well would
        /// double-encode on the way out (&amp;amp;).
        /// </summary>
        private void SanitizeForStorage(News news)
        {
            news.Content = _sanitizer.SanitizeHtml(news.Content);
            news.Summary = _sanitizer.ToPlainText(news.Summary, 500);
            news.Title = _sanitizer.ToPlainText(news.Title, 250);
        }
    }
}