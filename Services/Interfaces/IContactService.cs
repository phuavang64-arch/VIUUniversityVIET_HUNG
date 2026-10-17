using VIUUniversity.Models;

namespace VIUUniversity.Services.Interfaces
{
    public interface IContactService
    {
        Task SubmitContactMessageAsync(ContactMessage message);
        Task<List<Partner>> GetPartnersAsync();
        Task<List<Campus>> GetCampusesAsync();
    }
}