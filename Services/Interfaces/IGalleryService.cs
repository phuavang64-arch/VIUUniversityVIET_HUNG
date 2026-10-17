using VIUUniversity.Models;

namespace VIUUniversity.Services.Interfaces
{
    public interface IGalleryService
    {
        Task<List<GalleryImage>> GetImagesAsync(string? album = null, int take = 0);
    }
}