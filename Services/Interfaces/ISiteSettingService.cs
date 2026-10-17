using VIUUniversity.Models;

namespace VIUUniversity.Services.Interfaces
{
    public interface ISiteSettingService
    {
        Task<string> GetAsync(string key, string fallback = "");
        Task<Dictionary<string, string>> GetAllAsync();
        Task<List<SiteSetting>> GetAllEntitiesAsync();
        Task UpsertAsync(IEnumerable<SiteSetting> settings);
    }
}