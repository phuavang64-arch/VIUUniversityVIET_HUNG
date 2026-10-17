using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    /// <summary>Quản trị chuyên mục Hợp tác quốc tế và doanh nghiệp.</summary>
    [Area("Admin")]
    [Route("admin/hop-tac")]
    [Authorize(Roles = "Admin,Editor")]
    public class CooperationsController : SectionContentControllerBase
    {
        public CooperationsController(
            INewsService newsService, ApplicationDbContext db, IWebHostEnvironment env)
            : base(newsService, db, env)
        {
        }

        protected override string Section => ContentSection.HopTac;
        protected override string Label => "Hợp tác";
        protected override string UploadFolder => "cooperation";
    }
}