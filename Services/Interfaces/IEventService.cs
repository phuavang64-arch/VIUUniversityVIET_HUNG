using VIUUniversity.Models;
using VIUUniversity.ViewModels;

namespace VIUUniversity.Services.Interfaces
{
    public interface IEventService
    {
        Task<PagedResult<Event>> GetPublishedAsync(int page, int pageSize);
        Task<List<Event>> GetUpcomingAsync(int count);
        Task<Event?> GetBySlugAsync(string slug);

        Task<PagedResult<Event>> GetAllForAdminAsync(int page, int pageSize);
        Task<Event?> GetByIdAsync(int id);
        Task<Event> CreateAsync(Event item);
        Task UpdateAsync(Event item);
        Task DeleteAsync(int id);
    }
}
