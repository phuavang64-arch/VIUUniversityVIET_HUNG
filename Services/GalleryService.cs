using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    public class GalleryService : IGalleryService
    {
        private readonly ApplicationDbContext _db;

        public GalleryService(ApplicationDbContext db) => _db = db;

        public async Task<List<GalleryImage>> GetImagesAsync(string? album = null, int take = 0)
        {
            var query = _db.GalleryImages.AsNoTracking().Where(g => !g.IsDeleted && g.IsActive);

            if (!string.IsNullOrWhiteSpace(album))
                query = query.Where(g => g.Album == album);

            query = query.OrderBy(g => g.DisplayOrder).ThenByDescending(g => g.CreatedAt);

            if (take > 0)
                query = query.Take(take);

            return await query.ToListAsync();
        }
    }
}