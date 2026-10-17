using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    /// <summary>
    /// Rule-based admission assistant. Not a language model: it matches the visitor's
    /// question against a set of keywords and answers from live database data.
    ///
    /// The reply is HTML rendered with innerHTML by the chat widget, so every value that
    /// comes from the database (or from the visitor's own question) is HTML-encoded here.
    /// Encoding at the boundary is what makes the client-side render safe.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ISiteSettingService _settings;
        private readonly IContentSanitizer _sanitizer;

        public AiController(ApplicationDbContext db, ISiteSettingService settings, IContentSanitizer sanitizer)
        {
            _db = db;
            _settings = settings;
            _sanitizer = sanitizer;
        }

        [HttpGet("ask")]
        [EnableRateLimiting(RateLimitPolicies.Chat)]
        public async Task<IActionResult> Ask([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new
                {
                    reply = "Xin chào! Tôi là Trợ lý Tuyển sinh VIU. Bạn có thể hỏi tôi về điểm chuẩn, học phí, các ngành đào tạo, phương thức xét tuyển, cơ sở đào tạo hoặc ký túc xá."
                });
            }

            var question = q.Trim().ToLowerInvariant();
            var qNorm = RemoveDiacritics(question);

            // 1. Điểm chuẩn trúng tuyển
            if (qNorm.Contains("diem chuan") || qNorm.Contains("diem trung tuyen") || qNorm.Contains("diem thi") || qNorm.Contains("bao nhieu diem"))
            {
                var majors = await _db.AdmissionMajors
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderByDescending(m => m.Score2025)
                    .Take(6)
                    .ToListAsync();

                if (majors.Count == 0)
                    return Ok(new { reply = "Hiện chưa có dữ liệu điểm chuẩn. Vui lòng liên hệ ban Tuyển sinh." });

                var listText = string.Join("<br>", majors.Select(m =>
                    $"• <strong>{E(m.MajorName)}</strong> ({E(m.MajorCode)}): <strong>{(m.Score2025?.ToString("0.0") ?? "-")}</strong> điểm (Tổ hợp: {E(m.SubjectCombinations)})"));

                return Ok(new
                {
                    reply = $"🎯 <strong>Điểm chuẩn trúng tuyển năm 2025 gần nhất:</strong><br>{listText}<br><br><a href='/tuyen-sinh/tra-cuu-diem-chuan'>→ Tra cứu điểm chuẩn chi tiết tất cả các ngành</a>"
                });
            }

            // 2. Học phí
            if (qNorm.Contains("hoc phi") || qNorm.Contains("tien hoc") || qNorm.Contains("chi phi") || qNorm.Contains("kinh phi"))
            {
                var majors = await _db.Majors
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderBy(m => m.Tuition)
                    .ToListAsync();

                if (majors.Count == 0)
                    return Ok(new { reply = "Hiện chưa có dữ liệu học phí. Vui lòng liên hệ ban Tuyển sinh." });

                var tuitionList = string.Join("<br>", majors.Select(m =>
                    $"• <strong>{E(m.Name)}</strong>: {m.Tuition:N0} đ/năm"));

                var scholarshipNote = await _settings.GetAsync("admission.scholarship.note", string.Empty);
                var noteHtml = string.IsNullOrWhiteSpace(scholarshipNote)
                    ? string.Empty
                    : $"<br><br><em>{E(scholarshipNote)}</em>";

                return Ok(new
                {
                    reply = $"💰 <strong>Học phí các ngành tại Trường ĐH Công nghiệp Việt - Hung:</strong><br>{tuitionList}{noteHtml}"
                });
            }

            // 3. Ngành đào tạo
            if (qNorm.Contains("nganh") || qNorm.Contains("chuyen nganh") || qNorm.Contains("dao tao") || qNorm.Contains("hoc gi"))
            {
                var majors = await _db.Majors
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderBy(m => m.Name)
                    .ToListAsync();

                var majorNames = string.Join("<br>", majors.Select(m =>
                    $"• <a href='/dao-tao/nganh/{Uri.EscapeDataString(m.Slug)}'>{E(m.Name)}</a> (Mã: {E(m.Code)} - {E(m.DegreeLevel)})"));

                return Ok(new
                {
                    reply = $"🎓 <strong>Các ngành đào tạo đại học chính quy tại VIU:</strong><br>{majorNames}<br><br><a href='/dao-tao'>→ Xem chi tiết chương trình đào tạo &amp; khung môn học</a>"
                });
            }

            // 4. Phương thức xét tuyển & Đăng ký
            if (qNorm.Contains("xet tuyen") || qNorm.Contains("phuong thuc") || qNorm.Contains("dang ky") || qNorm.Contains("hoc ba") || qNorm.Contains("ho so"))
            {
                var methods = await _db.AdmissionMethods
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();

                var methodsText = string.Join("<br>", methods.Select((m, i) =>
                    $"{i + 1}. <strong>{E(m.Name)}</strong>: {E(m.Description)}"));

                return Ok(new
                {
                    reply = $"📝 <strong>Các phương thức tuyển sinh:</strong><br>{methodsText}<br><br><a href='/tuyen-sinh#form-dang-ky'>Đăng ký xét tuyển trực tuyến ngay</a>"
                });
            }

            // 5. Cơ sở / Địa chỉ / Địa điểm học
            if (qNorm.Contains("co so") || qNorm.Contains("dia chi") || qNorm.Contains("o dau") || qNorm.Contains("dia diem"))
            {
                var campuses = await _db.Campuses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                var campusText = string.Join("<br>", campuses.Select(c =>
                    $"📍 <strong>{E(c.Name)}</strong>: {E(c.Address)}"));

                return Ok(new
                {
                    reply = $"🏛️ <strong>Hệ thống cơ sở đào tạo của VIU:</strong><br>{campusText}"
                });
            }

            // 6. Ký túc xá & Học bổng
            if (qNorm.Contains("ky tuc xa") || qNorm.Contains("ktx") || qNorm.Contains("hoc bong") || qNorm.Contains("cho o"))
            {
                var dorm = await _settings.GetAsync("dorm.description", string.Empty);
                var dormHtml = string.IsNullOrWhiteSpace(dorm)
                    ? "Thông tin ký túc xá đang được cập nhật."
                    : E(dorm);

                return Ok(new
                {
                    reply = $"🏢 <strong>Ký túc xá &amp; Học bổng VIU:</strong><br>{dormHtml}<br><a href='/tuyen-sinh#form-dang-ky'>→ Đăng ký xét tuyển &amp; xét ký túc xá</a>"
                });
            }

            // 7. Tin tức mới nhất
            if (qNorm.Contains("tin tuc") || qNorm.Contains("tin moi") || qNorm.Contains("su kien"))
            {
                var news = await _db.News
                    .AsNoTracking()
                    .Where(n => !n.IsDeleted && n.IsPublished)
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(3)
                    .ToListAsync();

                var newsText = string.Join("<br>", news.Select(n =>
                    $"📰 <a href='/tin-tuc/{Uri.EscapeDataString(n.Slug)}'><strong>{E(n.Title)}</strong></a> ({n.PublishedAt:dd/MM/yyyy})"));

                return Ok(new
                {
                    reply = $"📢 <strong>Tin tức mới nhất từ nhà trường:</strong><br>{newsText}<br><br><a href='/tin-tuc'>→ Xem tất cả tin tức</a>"
                });
            }

            // 8. Liên hệ hotline
            if (qNorm.Contains("lien he") || qNorm.Contains("hotline") || qNorm.Contains("sdt") || qNorm.Contains("so dien thoai") || qNorm.Contains("email"))
            {
                var hotline = await _settings.GetAsync("contact.hotline", "024.3383.8345");
                var email = await _settings.GetAsync("contact.email", "tuyensinh@viu.edu.vn");
                var address = await _settings.GetAsync("contact.address", string.Empty);

                var addressHtml = string.IsNullOrWhiteSpace(address) ? string.Empty : $"<br>• Địa chỉ: <strong>{E(address)}</strong>";

                return Ok(new
                {
                    reply = $"📞 <strong>Kênh liên hệ chính thức VIU:</strong><br>• Hotline Tuyển sinh: <strong>{E(hotline)}</strong><br>• Email tư vấn: <strong>{E(email)}</strong>{addressHtml}<br><a href='/lien-he'>→ Tới trang Liên hệ</a>"
                });
            }

            // 9. Thống kê / quy mô trường — số liệu lấy từ Cài đặt trong Admin CMS
            if (qNorm.Contains("thong ke") || qNorm.Contains("quy mo") || qNorm.Contains("bao nhiieu giang vien") || qNorm.Contains("co bao nhiêu") || qNorm.Contains("so luong"))
            {
                var stats = await _settings.GetAllAsync();

                var lecturerCount = await _db.Lecturers.CountAsync(l => !l.IsDeleted && l.IsActive);
                var majorCount = await _db.Majors.CountAsync(m => !m.IsDeleted && m.IsActive);
                var deptCount = await _db.Departments.CountAsync(d => !d.IsDeleted && d.IsActive);

                var lines = new List<string>
                {
                    $"• Khoa đào tạo: <strong>{deptCount}</strong>",
                    $"• Ngành đào tạo: <strong>{majorCount}</strong>"
                };

                if (stats.TryGetValue("stats.lecturerTotal", out var lecturerTotal) && !string.IsNullOrWhiteSpace(lecturerTotal))
                    lines.Add($"• Giảng viên: <strong>{E(lecturerTotal)}</strong>");

                if (stats.TryGetValue("stats.partnerTotal", out var partnerTotal) && !string.IsNullOrWhiteSpace(partnerTotal))
                    lines.Add($"• Doanh nghiệp đối tác: <strong>{E(partnerTotal)}</strong>");

                if (stats.TryGetValue("stats.employmentRate", out var employment) && !string.IsNullOrWhiteSpace(employment))
                    lines.Add($"• Tỷ lệ có việc làm sau 12 tháng: <strong>{E(employment)}</strong>");

                return Ok(new
                {
                    reply = $"📊 <strong>Quy mô đào tạo tại VIU:</strong><br>{string.Join("<br>", lines)}"
                });
            }

            // Default fallback — echoes the visitor's own question, so it MUST be encoded.
            var fallbackHotline = await _settings.GetAsync("contact.hotline", "024.3383.8345");

            return Ok(new
            {
                reply = $"🤖 Cảm ơn câu hỏi của bạn về <em>\"{E(q)}\"</em>.<br>Để được tư vấn nhanh và chính xác nhất, bạn có thể gọi hotline: <strong>{E(fallbackHotline)}</strong> hoặc để lại thông tin tại <a href='/tuyen-sinh#form-dang-ky'>Form Đăng ký tư vấn trực tuyến</a> để ban tư vấn tuyển sinh gọi lại hỗ trợ ngay nhé!"
            });
        }

        /// <summary>HTML-encodes any value that is interpolated into the reply markup.</summary>
        private string E(string? value) => _sanitizer.Encode(value);

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC)
                .Replace("đ", "d").Replace("Đ", "d");
        }
    }
}