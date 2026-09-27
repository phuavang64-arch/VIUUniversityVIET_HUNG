using VIUUniversity.Models;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<PagedResult<Announcement>> GetPublishedAsync(int page, int pageSize);
        Task<List<Announcement>> GetLatestAsync(int count);
        Task<Announcement?> GetByIdAsync(int id);

        Task<PagedResult<Announcement>> GetAllForAdminAsync(int page, int pageSize);
        Task<Announcement> CreateAsync(Announcement item);
        Task UpdateAsync(Announcement item);
        Task DeleteAsync(int id);
    }
}
