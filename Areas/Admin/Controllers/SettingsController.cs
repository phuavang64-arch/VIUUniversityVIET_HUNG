using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Models;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    /// <summary>
    /// Editable site-wide values (hotline, address, the figures the chatbot quotes).
    ///
    /// These used to be hardcoded in the chat widget script, where nobody could change or
    /// verify them. Anything left blank is simply not displayed, which is how an
    /// unverified statistic gets hidden rather than published.
    /// </summary>
    [Area("Admin")]
    [Route("admin/cai-dat")]
    [Authorize(Roles = "Admin,Editor")]
    public class SettingsController : Controller
    {
        private readonly ISiteSettingService _settings;

        public SettingsController(ISiteSettingService settings)
        {
            _settings = settings;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var settings = await _settings.GetAllEntitiesAsync();
            ViewData["Title"] = "Cài đặt website";
            return View(settings);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(List<SiteSetting> settings)
        {
            // Internal bookkeeping rows must not be editable from the UI.
            var editable = settings
                .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !s.Key.StartsWith("seed."))
                .Select(s => new SiteSetting { Key = s.Key, Value = s.Value })
                .ToList();

            await _settings.UpsertAsync(editable);

            TempData["Success"] = "Đã lưu cài đặt.";
            return RedirectToAction(nameof(Index));
        }
    }
}