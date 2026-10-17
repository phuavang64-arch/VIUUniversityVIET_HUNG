using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Services.Interfaces;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services
{
    /// <summary>
    /// Global search across news, announcements, events and majors.
    ///
    /// This used to live inside HomeController, which meant the controller held a direct
    /// ApplicationDbContext and the same four queries were written twice (once for the
    /// dropdown, once for the results page).
    ///
    /// Note on performance: "x.ToLower().Contains(k)" cannot use an index on any provider,
    /// so these queries are full scans. That is acceptable at the current data volume; if
    /// content grows into the tens of thousands of rows, switch to a normalised
    /// search column with a computed index, or a full-text index (FTS5 on SQLite /
    /// SQL Server full-text).
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _db;

        public SearchService(ApplicationDbContext db) => _db = db;

        public async Task<SearchPageViewModel> SearchAsync(string? query, int takePerType)
        {
            var vm = new SearchPageViewModel { Query = query?.Trim() };
            if (string.IsNullOrWhiteSpace(vm.Query))
                return vm;

            var keyword = vm.Query.ToLowerInvariant();

            var news = await _db.News
                .AsNoTracking()
                .Where(n => !n.IsDeleted && n.IsPublished &&
                    (n.Title.ToLower().Contains(keyword) ||
                     (n.Summary != null && n.Summary.ToLower().Contains(keyword)) ||
                     n.Content.ToLower().Contains(keyword)))
                .OrderByDescending(n => n.PublishedAt)
                .Take(takePerType)
                .Select(n => new SearchResultItem
                {
                    Title = n.Title,
                    Summary = n.Summary,
                    Type = "Tin tức",
                    Url = "/tin-tuc/" + n.Slug,
                    Date = n.PublishedAt != null ? n.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                    BadgeClass = "bg-primary"
                })
                .ToListAsync();

            var announcements = await _db.Announcements
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.IsPublished &&
                    (a.Title.ToLower().Contains(keyword) || a.Content.ToLower().Contains(keyword)))
                .OrderByDescending(a => a.PublishedAt)
                .Take(takePerType)
                .Select(a => new SearchResultItem
                {
                    Title = a.Title,
                    Summary = null,
                    Type = "Thông báo",
                    Url = "/thong-bao/" + a.Id,
                    Date = a.PublishedAt != null ? a.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                    BadgeClass = "bg-danger"
                })
                .ToListAsync();

            var events = await _db.Events
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.IsPublished &&
                    (e.Title.ToLower().Contains(keyword) ||
                     (e.Description != null && e.Description.ToLower().Contains(keyword))))
                .OrderByDescending(e => e.StartAt)
                .Take(takePerType)
                .Select(e => new SearchResultItem
                {
                    Title = e.Title,
                    Summary = e.Location,
                    Type = "Sự kiện",
                    Url = "/su-kien/" + e.Slug,
                    Date = e.StartAt.ToString("dd/MM/yyyy"),
                    BadgeClass = "bg-success"
                })
                .ToListAsync();

            var majors = await _db.Majors
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.IsActive &&
                    (m.Name.ToLower().Contains(keyword) || m.Code.ToLower().Contains(keyword) ||
                     (m.Description != null && m.Description.ToLower().Contains(keyword))))
                .Take(takePerType)
                .Select(m => new SearchResultItem
                {
                    Title = m.Name + " (" + m.Code + ")",
                    Summary = m.Description,
                    Type = "Ngành học",
                    Url = "/dao-tao/nganh/" + m.Slug,
                    Date = m.DegreeLevel ?? "Đại học",
                    BadgeClass = "bg-warning text-dark"
                })
                .ToListAsync();

            vm.NewsCount = news.Count;
            vm.AnnouncementCount = announcements.Count;
            vm.EventCount = events.Count;
            vm.MajorCount = majors.Count;
            vm.Results = news.Concat(announcements).Concat(events).Concat(majors).ToList();

            return vm;
        }

        public async Task<object> SearchApiAsync(string? query, int takePerType)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new
                {
                    news = Array.Empty<object>(),
                    announcements = Array.Empty<object>(),
                    majors = Array.Empty<object>(),
                    events = Array.Empty<object>()
                };
            }

            var keyword = query.Trim().ToLower();

            var newsList = await _db.News
                .AsNoTracking()
                .Where(n => !n.IsDeleted && n.IsPublished &&
                    (n.Title.ToLower().Contains(keyword) ||
                     (n.Summary != null && n.Summary.ToLower().Contains(keyword))))
                .OrderByDescending(n => n.PublishedAt)
                .Take(takePerType)
                .Select(n => new
                {
                    title = n.Title,
                    slug = n.Slug,
                    date = n.PublishedAt != null ? n.PublishedAt.Value.ToString("dd/MM/yyyy") : "",
                    type = "Tin tức",
                    url = "/tin-tuc/" + n.Slug
                })
                .ToListAsync();

            var announceList = await _db.Announcements
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.IsPublished && a.Title.ToLower().Contains(keyword))
                .OrderByDescending(a => a.PublishedAt)
                .Take(takePerType)
                .Select(a => new
                {
                    title = a.Title,
                    slug = a.Id.ToString(),
                    date = a.PublishedAt != null ? a.PublishedAt.Value.ToString("dd/MM/yyyy") : "",
                    type = "Thông báo",
                    url = "/thong-bao/" + a.Id
                })
                .ToListAsync();

            var majorList = await _db.Majors
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.IsActive &&
                    (m.Name.ToLower().Contains(keyword) || m.Code.ToLower().Contains(keyword) ||
                     (m.Description != null && m.Description.ToLower().Contains(keyword))))
                .Take(takePerType)
                .Select(m => new
                {
                    title = m.Name + " (" + m.Code + ")",
                    slug = m.Slug,
                    date = m.DegreeLevel ?? "Đại học",
                    type = "Ngành học",
                    url = "/dao-tao/nganh/" + m.Slug
                })
                .ToListAsync();

            var eventList = await _db.Events
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.IsPublished &&
                    (e.Title.ToLower().Contains(keyword) ||
                     (e.Description != null && e.Description.ToLower().Contains(keyword))))
                .OrderByDescending(e => e.StartAt)
                .Take(takePerType)
                .Select(e => new
                {
                    title = e.Title,
                    slug = e.Slug,
                    date = e.StartAt.ToString("dd/MM/yyyy"),
                    type = "Sự kiện",
                    url = "/su-kien/" + e.Slug
                })
                .ToListAsync();

            return new { news = newsList, announcements = announceList, majors = majorList, events = eventList };
        }
    }
}