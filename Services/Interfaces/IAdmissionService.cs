using VIUUniversity.Models;

namespace VIUUniversity.Services.Interfaces
{
    public interface IAdmissionService
    {
        Task<List<AdmissionMethod>> GetAdmissionMethodsAsync();
        Task<List<AdmissionMajor>> GetAdmissionMajorsAsync(string? keyword = null);
        Task<bool> SubmitLeadAsync(AdmissionLead lead);
        Task<bool> SubmitContactMessageAsync(ContactMessage message);
    }
}
