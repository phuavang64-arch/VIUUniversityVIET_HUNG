using VIUUniversity.ViewModels;

namespace VIUUniversity.Services.Interfaces
{
    public interface ISearchService
    {
        /// <summary>Full results for the /tim-kiem page, grouped and counted per content type.</summary>
        Task<SearchPageViewModel> SearchAsync(string? query, int takePerType);

        /// <summary>Compact payload for the header search dropdown (JSON API).</summary>
        Task<object> SearchApiAsync(string? query, int takePerType);
    }
}