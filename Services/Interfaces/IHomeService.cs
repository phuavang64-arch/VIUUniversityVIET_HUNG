using VIUUniversity.ViewModels;

namespace VIUUniversity.Services.Interfaces
{
    public interface IHomeService
    {
        Task<HomeViewModel> BuildHomeViewModelAsync();
    }
}