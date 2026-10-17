using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Areas.Admin.Controllers
{
    /// <summary>Quản trị chuyên mục Nghiên cứu khoa học. Dùng lại toàn bộ CRUD của Tin tức.</summary>
    [Area("Admin")]
    [Route("admin/nghien-cuu")]
    [Authorize(Roles = "Admin,Editor")]
    public class ResearchController : SectionContentControllerBase
    {
        public ResearchController(
            INewsService newsService, ApplicationDbContext db, IWebHostEnvironment env)
            : base(newsService, db, env)
        {
        }

        protected override string Section => ContentSection.NghiênCứu;
        protected override string Label => "Nghiên cứu khoa học";
        protected override string UploadFolder => "research";
    }
}