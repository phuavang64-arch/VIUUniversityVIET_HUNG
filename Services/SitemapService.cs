using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    /// <summary>
    /// Builds sitemap.xml straight from the database so the file can never drift out of
    /// date the way a hand-maintained list would: publishing an article and refreshing
    /// the sitemap are the same act.
    /// </summary>
    public class SitemapService : ISitemapService
    {
        private readonly ApplicationDbContext _db;

        public SitemapService(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<SitemapEntry>> BuildEntriesAsync()
        {
            var entries = new List<SitemapEntry>();

            // ---- Trang tĩnh ----
            // These only change when the site is rebuilt, so they carry no lastmod:
            // claiming one would be a guess.
            void AddStatic(string path, double priority) =>
                entries.Add(new SitemapEntry(path, null, "weekly", priority));

            AddStatic("/", 1.0);
            AddStatic("/gioi-thieu", 0.9);
            AddStatic("/nghien-cuu", 0.8);
            AddStatic("/hop-tac", 0.7);
            AddStatic("/sinh-vien", 0.8);
            AddStatic("/dao-tao", 0.9);
            AddStatic("/dao-tao/giang-vien", 0.6);
            AddStatic("/tuyen-sinh", 0.9);
            AddStatic("/tuyen-sinh/tra-cuu-diem-chuan", 0.8);
            AddStatic("/tuyen-sinh/tinh-diem", 0.7);
            AddStatic("/hoc-phi", 0.7);
            AddStatic("/tin-tuc", 0.8);
            AddStatic("/thong-bao", 0.8);
            AddStatic("/su-kien", 0.7);
            AddStatic("/lien-he", 0.6);

            // ---- Nội dung do biên tập viên quản lý ----
            // Only published, non-deleted rows are listed: anything else would advertise
            // a draft URL to search engines and then 404 when it is unpublished.

            // Bốn chuyên mục dùng chung bảng News nhưng có bốn đường dẫn khác nhau. Sitemap
            // phải dẫn tới đúng đường dẫn của chuyên mục đó, nếu không Google sẽ
            // thấy URL 404. Xem ContentSection.
            var news = await _db.News
                .AsNoTracking()
                .Where(n => n.IsPublished && !n.IsDeleted)
                .Select(n => new { n.Slug, n.Section, n.PublishedAt })
                .ToListAsync();

            foreach (var item in news)
            {
                entries.Add(new SitemapEntry(
                    SectionBasePath(item.Section) + Uri.EscapeDataString(item.Slug),
                    item.PublishedAt,
                    "monthly",
                    0.6));
            }

            var announcements = await _db.Announcements
                .AsNoTracking()
                .Where(a => a.IsPublished && !a.IsDeleted)
                .Select(a => new { a.Id, a.PublishedAt })
                .ToListAsync();

            foreach (var item in announcements)
            {
                entries.Add(new SitemapEntry(
                    "/thong-bao/" + item.Id,
                    item.PublishedAt,
                    "weekly",
                    0.6));
            }

            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.IsPublished && !e.IsDeleted)
                .Select(e => new { e.Slug, e.StartAt })
                .ToListAsync();

            foreach (var item in events)
            {
                entries.Add(new SitemapEntry(
                    "/su-kien/" + Uri.EscapeDataString(item.Slug),
                    item.StartAt,
                    "monthly",
                    0.5));
            }

            var departments = await _db.Departments
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .Select(d => new { d.Slug })
                .ToListAsync();

            foreach (var item in departments)
            {
                entries.Add(new SitemapEntry(
                    "/dao-tao/khoa/" + Uri.EscapeDataString(item.Slug),
                    null,
                    "monthly",
                    0.7));
            }

            var majors = await _db.Majors
                .AsNoTracking()
                .Where(m => !m.IsDeleted)
                .Select(m => new { m.Slug })
                .ToListAsync();

            foreach (var item in majors)
            {
                entries.Add(new SitemapEntry(
                    "/dao-tao/nganh/" + Uri.EscapeDataString(item.Slug),
                    null,
                    "monthly",
                    0.7));
            }

            return entries;
        }

        /// <summary>
        /// Đường dẫn gốc của một chuyên mục bài viết. Giá trị lạ (rác trong cột
        /// Section) rơi về Tin tức để sitemap không bị đứt.
        /// </summary>
        private static string SectionBasePath(string? section) => section switch
        {
            ContentSection.NghiênCứu => "/nghien-cuu/",
            ContentSection.HopTac => "/hop-tac/",
            ContentSection.HocPhi => "/hoc-phi/",
            _ => "/tin-tuc/"
        };
    }
}