using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services.Chat
{
    public record KnowledgeContext(
        string Text,
        List<string> Facts,
        bool HasTopicFacts,
        List<ChatLink> Links,
        List<string> FollowUps);

    /// <summary>
    /// Builds the factual context handed to the model.
    ///
    /// The model never queries the database. Facts are gathered here with fixed,
    /// parameterised EF Core queries over public tables only, so there is no path by
    /// which a prompt can trigger arbitrary SQL or reach administrative data
    /// (leads, contact messages, audit logs, user accounts are never touched).
    /// </summary>
    public class KnowledgeService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISiteSettingService _settings;

        public KnowledgeService(ApplicationDbContext db, ISiteSettingService settings)
        {
            _db = db;
            _settings = settings;
        }

        /// <summary>
        /// Keyword routing decides which slice of the dataset is relevant. This is
        /// deliberately simple and deterministic: the model gets the school's real
        /// numbers, and only the phrasing of the answer is the model's job.
        /// </summary>
        public async Task<KnowledgeContext> BuildAsync(string question, CancellationToken ct)
        {
            var q = RemoveDiacritics(question.ToLowerInvariant());
            var facts = new List<string>();
            var links = new List<ChatLink>();
            var followUps = new List<string>();

            if (Matches(q, "diem chuan", "diem trung tuyen", "diem thi", "bao nhieu diem", "cut diem"))
            {
                var rows = await _db.AdmissionMajors.AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderByDescending(m => m.Score2025)
                    .Take(10)
                    .ToListAsync(ct);

                if (rows.Count > 0)
                {
                    facts.Add("ĐIỂM CHUẨN TRÚNG TUYỂN (dữ liệu trường):");
                    foreach (var m in rows)
                    {
                        facts.Add($"- Ngành {m.MajorName} (mã {m.MajorCode}): " +
                                  $"năm 2023 = {Fmt(m.Score2023)}, 2024 = {Fmt(m.Score2024)}, 2025 = {Fmt(m.Score2025)} điểm. " +
                                  $"Tổ hợp: {m.SubjectCombinations}. Chỉ tiêu: {m.Quota?.ToString() ?? "chưa cập nhật"}.");
                    }
                }

                links.Add(new ChatLink("Tra cứu điểm chuẩn đầy đủ", "/tuyen-sinh/tra-cuu-diem-chuan"));
                followUps.Add("Phương thức xét tuyển nào phù hợp với tôi?");
                followUps.Add("Học phí ngành Công nghệ thông tin là bao nhiêu?");
            }

            if (Matches(q, "hoc phi", "tien hoc", "chi phi", "kinh phi", "bao nhieu tien"))
            {
                var rows = await _db.Majors.AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderBy(m => m.Tuition)
                    .ToListAsync(ct);

                if (rows.Count > 0)
                {
                    facts.Add("HỌC PHÍ (dữ liệu trường):");
                    foreach (var m in rows)
                    {
                        facts.Add($"- {m.Name} (mã {m.Code}): {m.Tuition:N0} đồng/năm, thời gian {m.DurationYears} năm.");
                    }
                }

                var note = await _settings.GetAsync("admission.scholarship.note", string.Empty);
                if (!string.IsNullOrWhiteSpace(note))
                    facts.Add($"Ghi chú học bổng của trường: {note}");

                links.Add(new ChatLink("Danh sách ngành đào tạo", "/dao-tao"));
                followUps.Add("Điểm chuẩn ngành Kế toán năm 2025?");
            }

            if (Matches(q, "nganh", "chuyen nganh", "dao tao", "hoc gi", "khoa", "chuyen nghe", "lam gi"))
            {
                var majors = await _db.Majors.AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .Include(m => m.Department)
                    .OrderBy(m => m.Name)
                    .ToListAsync(ct);

                var depts = await _db.Departments.AsNoTracking()
                    .Where(d => !d.IsDeleted && d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync(ct);

                if (depts.Count > 0)
                {
                    facts.Add("CÁC KHOA ĐANG MỞ (dữ liệu trường):");
                    foreach (var d in depts)
                    {
                        var majorsOfDept = majors.Where(m => m.DepartmentId == d.Id).ToList();
                        facts.Add($"- {d.Name} (mã {d.Code}): {d.Description ?? "chưa có mô tả"}. " +
                                  $"Ngành đào tạo: {string.Join(", ", majorsOfDept.Select(m => m.Name))}.");
                    }
                }

                links.Add(new ChatLink("Tổng quan đào tạo", "/dao-tao"));
                followUps.Add("Học phí và điểm chuẩn của ngành Kỹ thuật phần mềm?");
            }

            if (Matches(q, "xet tuyen", "phuong thuc", "dang ky", "hoc ba", "ho so", "thi tuyen"))
            {
                var methods = await _db.AdmissionMethods.AsNoTracking()
                    .Where(m => !m.IsDeleted && m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync(ct);

                if (methods.Count > 0)
                {
                    facts.Add("PHƯƠNG THỨC TUYỂN SINH (dữ liệu trường):");
                    foreach (var m in methods)
                    {
                        facts.Add($"- {m.Name}: {m.Description ?? ""} Yêu cầu: {m.Requirements ?? "chưa cập nhật"}.");
                    }
                }

                links.Add(new ChatLink("Đề án tuyển sinh", "/tuyen-sinh"));
                links.Add(new ChatLink("Đăng ký tư vấn", "/tuyen-sinh#form-dang-ky"));
                followUps.Add("Điểm chuẩn năm 2025 của các ngành?");
            }

            if (Matches(q, "co so", "dia chi", "o dau", "dia diem", "dia chi truong"))
            {
                var campuses = await _db.Campuses.AsNoTracking()
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(ct);

                if (campuses.Count > 0)
                {
                    facts.Add("HỆ THỐNG CƠ SỞ (dữ liệu trường):");
                    foreach (var c in campuses)
                    {
                        facts.Add($"- {c.Name}: {c.Address ?? "chưa cập nhật địa chỉ"}{(c.Phone is null ? "" : $". Điện thoại: {c.Phone}")}.");
                    }
                }

                links.Add(new ChatLink("Thông tin liên hệ", "/lien-he"));
                followUps.Add("Trường có những phương thức xét tuyển nào?");
            }

            if (Matches(q, "tin tuc", "tin moi", "su kien", "thong bao"))
            {
                var news = await _db.News.AsNoTracking()
                    .Where(n => !n.IsDeleted && n.IsPublished)
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(4)
                    .ToListAsync(ct);

                if (news.Count > 0)
                {
                    facts.Add("TIN TỨC MỚI NHẤT (dữ liệu trường):");
                    foreach (var n in news)
                    {
                        facts.Add($"- {n.Title} ({n.PublishedAt:dd/MM/yyyy}): /tin-tuc/{n.Slug}");
                    }
                }

                links.Add(new ChatLink("Tất cả tin tức", "/tin-tuc"));
                followUps.Add("Đề án tuyển sinh hiện tại thế nào?");
            }

            // Luôn kèm thông tin liên hệ để mọi câu trả lời đều có đường dẫn về con người thật.
            // Dòng này KHÔNG tính vào HasTopicFacts - nó không phải dữ liệu về chủ đề hỏi.
            var hotline = await _settings.GetAsync("contact.hotline", "024.3383.8345");
            var email = await _settings.GetAsync("contact.email", "tuyensinh@viu.edu.vn");
            facts.Add($"LIÊN HỆ: hotline tuyển sinh {hotline}, email {email}.");
            links.Add(new ChatLink("Liên hệ nhà trường", "/lien-he"));

            if (followUps.Count == 0)
            {
                followUps.Add("Điểm chuẩn tuyển sinh năm 2025 là bao nhiêu?");
                followUps.Add("Trường đang đào tạo những ngành nào?");
                followUps.Add("Học phí và phương thức xét tuyển thế nào?");
            }

            // facts.Count luôn >= 1 vì dòng liên hệ ở trên, nên phải đếm riêng
            // các nhánh chủ đề mới thực sự có dữ liệu.
            var topicFactCount = facts.Count - 1;
            var hasTopicFacts = topicFactCount > 0;

            var text = hasTopicFacts
                ? string.Join("\n", facts)
                : "KHÔNG có dữ liệu trường liên quan tới câu hỏi này.";

            return new KnowledgeContext(text, facts, hasTopicFacts, links.DistinctBy(l => l.Url).ToList(), followUps);
        }

        private static string Fmt(double? value) => value.HasValue ? value.Value.ToString("0.0") : "chưa công bố";

        private static bool Matches(string haystack, params string[] needles) =>
            needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC)
                .Replace("đ", "d").Replace("Đ", "d");
        }
    }
}