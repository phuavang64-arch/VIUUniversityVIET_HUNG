using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    public class SiteSettingService : ISiteSettingService
    {
        private readonly ApplicationDbContext _db;

        public SiteSettingService(ApplicationDbContext db) => _db = db;

        public async Task<string> GetAsync(string key, string fallback = "")
        {
            var value = await _db.SiteSettings
                .AsNoTracking()
                .Where(s => s.Key == key)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(value) ? fallback : value!;
        }

        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            var rows = await _db.SiteSettings
                .AsNoTracking()
                .Select(s => new { s.Key, s.Value })
                .ToListAsync();

            return rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Value))
                .GroupBy(r => r.Key)
                .ToDictionary(g => g.Key, g => g.First().Value!, StringComparer.OrdinalIgnoreCase);
        }

        public Task<List<SiteSetting>> GetAllEntitiesAsync() =>
            _db.SiteSettings.AsNoTracking().OrderBy(s => s.Group).ThenBy(s => s.Id).ToListAsync();

        public async Task UpsertAsync(IEnumerable<SiteSetting> settings)
        {
            var incoming = settings.ToList();
            if (incoming.Count == 0)
                return;

            var keys = incoming.Select(s => s.Key).ToList();
            var existing = await _db.SiteSettings.Where(s => keys.Contains(s.Key)).ToListAsync();

            foreach (var item in incoming)
            {
                var match = existing.FirstOrDefault(e => e.Key == item.Key);
                if (match == null)
                {
                    _db.SiteSettings.Add(new SiteSetting
                    {
                        Key = item.Key,
                        Value = item.Value,
                        Description = item.Description,
                        Group = item.Group
                    });
                }
                else
                {
                    match.Value = item.Value;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}