using VIUUniversity.Models;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services.Interfaces
{
    public interface INewsService
    {
        Task<PagedResult<News>> GetPublishedAsync(int page, int pageSize, string? keyword = null, int? categoryId = null);
        Task<News?> GetBySlugAsync(string slug);
        Task<List<News>> GetLatestAsync(int count);

        // ---- Admin CMS ----
        Task<PagedResult<News>> GetAllForAdminAsync(int page, int pageSize, string? keyword = null);
        Task<News?> GetByIdAsync(int id);
        Task<News> CreateAsync(News news);
        Task UpdateAsync(News news);
        Task DeleteAsync(int id);
        Task SetPublishStateAsync(int id, bool isPublished);
    }
}
