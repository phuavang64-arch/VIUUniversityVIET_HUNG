using VIUUniversity.Models;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services.Interfaces
{
    public interface INewsService
    {
        Task<PagedResult<News>> GetPublishedAsync(int page, int pageSize, string? keyword = null, int? categoryId = null);
        Task<News?> GetBySlugAsync(string slug);
        Task<List<News>> GetLatestAsync(int count);
        /// <param name="categoryId">
        /// Chuyên mục tin; null nghĩa là bài đang xem không có chuyên mục, khi đó
        /// lấy bài liên quan theo thứ tự mới nhất thay vì lọc theo danh mục.
        /// </param>
        Task<List<News>> GetRelatedAsync(int newsId, int? categoryId, int count = 3);

        // ---- Theo chuyên mục (Tin tức / Nghiên cứu KH / Hợp tác / Học phí) ----
        // Cùng một bảng News, chỉ khác điều kiện lọc. Dùng chung giúp cả bốn
        // chuyên mục có cùng phân trang, tìm kiếm, nháp/đăng và đếm lượt đọc.
        Task<PagedResult<News>> GetPublishedBySectionAsync(
            string section, int page, int pageSize, string? keyword = null);

        /// <summary>Lấy bài theo slug nhưng chỉ trong đúng chuyên mục đó.</summary>
        Task<News?> GetBySlugInSectionAsync(string slug, string section);

        Task<List<News>> GetRelatedInSectionAsync(string section, int newsId, int count = 3);

        Task<int> CountPublishedBySectionAsync(string section);

        // ---- Admin CMS ----
        Task<PagedResult<News>> GetAllForAdminAsync(int page, int pageSize, string? keyword = null);
        Task<PagedResult<News>> GetAllForAdminBySectionAsync(
            string section, int page, int pageSize, string? keyword = null);
        Task<News?> GetByIdAsync(int id);
        Task<News> CreateAsync(News news);
        Task UpdateAsync(News news);
        Task DeleteAsync(int id);
        Task SetPublishStateAsync(int id, bool isPublished);

        /// <summary>
        /// News.Slug carries a UNIQUE index. Without an explicit check, saving two articles
        /// that slugify to the same value throws a DbUpdateException and surfaces as a 500,
        /// which happens constantly when editors publish two posts with the same title.
        /// </summary>
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);

        /// <summary>
        /// Returns the slug unchanged when it is free, otherwise appends -2, -3, ... so the
        /// caller always has a value that satisfies the unique index.
        /// </summary>
        Task<string> EnsureUniqueSlugAsync(string slug, int? excludeId = null);
    }
}