using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Data
{
    /// <summary>
    /// Called once from Program.cs on startup. Safe to run repeatedly —
    /// every block checks "if already seeded" before inserting.
    /// </summary>
    public static class SeedData
    {
        /// <summary>
        /// Keys used to record "this block already ran" inside the SiteSettings table.
        /// Seeding must happen exactly once per block even when the rows it produced are
        /// later deleted by an administrator.
        /// </summary>
        private static class SeedMarkers
        {
            public const string AdmissionLeads = "seed.admissionleads.v1";
        }

        /// <summary>
        /// Cryptographically random password that satisfies the configured Identity policy
        /// (>= 10 chars, upper + lower + digit + non-alphanumeric).
        /// </summary>
        /// <summary>
        /// Nối các đoạn văn của một ngành thành chuỗi để lưu vào cột TEXT.
        /// Danh mục giữ từng đoạn riêng (string[]) để view dựng lại &lt;p&gt; cho
        /// đúng kiểu chữ; ở đây chỉ cần nội dung liền một khối.
        /// </summary>
        private static string Join(IEnumerable<string> paragraphs)
        {
            var list = paragraphs?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            return list is { Count: > 0 } ? string.Join("\n\n", list) : string.Empty;
        }

        private static string GenerateStrongPassword()
        {
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*-_=+";

            var all = lower + upper + digits + symbols;
            var chars = new List<char>();

            // Guarantee at least one character from each required class.
            chars.Add(lower[RandomNumberGenerator.GetInt32(lower.Length)]);
            chars.Add(upper[RandomNumberGenerator.GetInt32(upper.Length)]);
            chars.Add(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            chars.Add(symbols[RandomNumberGenerator.GetInt32(symbols.Length)]);

            while (chars.Count < 16)
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            // Fisher-Yates so the mandatory characters are not always in the same position.
            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private static Task<bool> HasSeedMarkerAsync(ApplicationDbContext db, string marker) =>
            db.SiteSettings.AsNoTracking().AnyAsync(s => s.Key == marker);

        private static async Task MarkSeededAsync(ApplicationDbContext db, string marker)
        {
            if (await HasSeedMarkerAsync(db, marker))
                return;

            db.SiteSettings.Add(new SiteSetting
            {
                Key = marker,
                Group = "system",
                Description = "Seed marker: dữ liệu mẫu đã được tạo. Đừng xoá dòng này."
            });

            await db.SaveChangesAsync();
        }
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();

            // Schema creation is DatabaseInitializer's job (real EF Core migrations).
            // This method only seeds data.

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // ---- 1. Roles ----
            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---- 2. Admin account ----
            var adminEmail = string.IsNullOrWhiteSpace(config["SeedAdmin:Email"])
                ? "admin@viu.edu.vn"
                : config["SeedAdmin:Email"]!.Trim();

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                // No hardcoded default password. This used to fall back to "Admin@123456"
                // which was also documented in the README, i.e. published in the repository.
                var adminPassword = config["SeedAdmin:Password"];

                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    if (!config.GetValue("ASPNETCORE_ENVIRONMENT", "Production").Equals("Development", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Chưa cấu hình SeedAdmin:Password. Hãy đặt bằng user-secrets hoặc biến môi trường " +
                            "trước khi khởi động ở môi trường thật: " +
                            "dotnet user-secrets set \"SeedAdmin:Password\" \"<mật-kh�u-mạnh>\"");
                    }

                    // Local development only: generate a usable password and print it once.
                    adminPassword = GenerateStrongPassword();
                    Console.WriteLine();
                    Console.WriteLine("=======================================================");
                    Console.WriteLine($" Tài khoản quản trị đã được tạo cho môi trường Development");
                    Console.WriteLine($" Email   : {adminEmail}");
                    Console.WriteLine($" Mật khẩu: {adminPassword}");
                    Console.WriteLine(" (tự sinh ngẫu nhiên - KHÔNG dùng lại ở production)");
                    Console.WriteLine("=======================================================");
                    Console.WriteLine();
                }

                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = string.IsNullOrWhiteSpace(config["SeedAdmin:FullName"])
                        ? "Quản trị hệ thống"
                        : config["SeedAdmin:FullName"]!
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Không tạo được tài khoản quản trị: " +
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }

                await userManager.AddToRoleAsync(admin, Roles.Admin);
            }

            // ---- 3. Departments & Majors ----
            if (!await db.Departments.AnyAsync())
            {
                var cntt = new Department
                {
                    Code = "CNTT",
                    Name = "Khoa Công nghệ thông tin",
                    Slug = SlugHelper.GenerateSlug("Khoa Công nghệ thông tin"),
                    Description = "Đào tạo nguồn nhân lực công nghệ thông tin với chương trình thực hành nặng, gắn với nhu cầu của doanh nghiệp.",
                    Email = "cntt@viu.edu.vn",
                    // Ảnh thật khoa: cổng "Khoa Công nghệ thông tin" tại cơ sở.
                    Image = "/images/departments/khoa-cntt.jpg",
                    Logo = "/images/departments/khoa-cntt.jpg"
                };
                var dien = new Department
                {
                    Code = "DIEN",
                    Name = "Khoa Điện - Điện tử",
                    Slug = SlugHelper.GenerateSlug("Khoa Điện - Điện tử"),
                    Description = "Đào tạo kỹ sư điện, điện tử và tự động hóa; sinh viên thực hành trên tủ điện và thiết bị đo lường thực tế.",
                    Email = "dien@viu.edu.vn",
                    // Ảnh thật khoa: sinh viên thực hành trên tủ điện tử.
                    Image = "/images/departments/khoa-dien-dien-tu.jpg",
                    Logo = "/images/departments/khoa-dien-dien-tu.jpg"
                };
                var kinhte = new Department
                {
                    Code = "KT",
                    Name = "Khoa Kinh tế",
                    Slug = SlugHelper.GenerateSlug("Khoa Kinh tế"),
                    Description = "Đào tạo cử nhân kinh tế, quản trị kinh doanh và kế toán theo hướng ứng dụng.",
                    Email = "kinhte@viu.edu.vn",
                    // Ảnh thật khoa: sinh viên Khoa Quản trị kinh doanh làm việc nhóm.
                    Image = "/images/departments/khoa-kinhte.jpg",
                    Logo = "/images/departments/khoa-kinhte.jpg"
                };
                db.Departments.AddRange(cntt, dien, kinhte);
                await db.SaveChangesAsync();

                // 21 ngành đại học chính quy K50: mã ngành, tên, ảnh và nội dung
                // lấy từ cổng tuyển sinh chính thức — xem MajorCatalogK50.
                var majors = MajorCatalogK50.All.Select(e => new Major
                {
                    Code = e.Code,
                    Name = e.Name,
                    Slug = e.Slug,
                    DepartmentId = e.DepartmentCode switch
                    {
                        "CNTT" => cntt.Id,
                        "DIEN" => dien.Id,
                        _ => kinhte.Id
                    },
                    DegreeLevel = "Đại học",
                    ImageUrl = e.ImageUrl,
                    SourceUrl = e.SourceUrl,
                    Description = Join(e.IntroParagraphs),
                    SkillsDescription = Join(e.SkillsParagraphs),
                    CareerDescription = Join(e.CareerParagraphs),
                    // Tuition để 0 = "Chưa công bố". Mức học phí đổi theo từng
                    // năm tuyển sinh và phải lấy từ văn bản chính thức; xem
                    // chuyên mục Học phí. Số bịa ở đây sẽ bị đọc là thông tin
                    // chính thức của Trường.
                    Tuition = 0,
                    // Thời gian đào tạo: giữ nguyên văn bản của Trường thay vì tự
                    // quy ra 4 hay 4,5 năm. DurationYears để 0 = chưa công bố số.
                    DurationYears = 0,
                    DurationDescription = Join(e.DurationParagraphs)
                }).ToList();

                db.Majors.AddRange(majors);
                await db.SaveChangesAsync();
            }

            // ---- 3b. Department imagery ----
            // Databases created before the faculty photos were added still point at the
            // old generic images. Backfill only when a department has no image at all, so
            // an administrator's later choice is never overwritten.
            var departmentImages = new Dictionary<string, string>
            {
                ["CNTT"] = "/images/departments/khoa-cntt.jpg",
                ["DIEN"] = "/images/departments/khoa-dien-dien-tu.jpg",
                ["KT"] = "/images/departments/khoa-kinhte.jpg"
            };

            var departmentsMissingImage = await db.Departments
                .Where(d => d.Image == null || d.Image == "")
                .ToListAsync();

            foreach (var dept in departmentsMissingImage)
            {
                if (departmentImages.TryGetValue(dept.Code, out var imagePath))
                {
                    dept.Image = imagePath;
                    if (string.IsNullOrWhiteSpace(dept.Logo))
                        dept.Logo = imagePath;
                }
            }

            if (departmentsMissingImage.Count > 0)
                await db.SaveChangesAsync();

            // ---- 3c. Đồng bộ danh mục 21 ngành đại học chính quy K50 ----
            // Chạy mỗi lần khởi động (không phụ thuộc bảng Majors có rỗng hay không)
            // để database cũ cũng có đủ 21 ngành mà không cần xoá dữ liệu.
            //
            // Nguyên tắc: danh mục là nguồn chuẩn cho mã ngành, tên ngành, ảnh và
            // nội dung giới thiệu. Chỉ ghi đè các trường đó, KHÔNG đụng tới dữ liệu
            // do quản trị nhập (không xoá, không đổi trạng thái).
            var departmentsByCode = await db.Departments.ToDictionaryAsync(d => d.Code);

            foreach (var e in MajorCatalogK50.All)
            {
                if (!departmentsByCode.TryGetValue(e.DepartmentCode, out var dept))
                    continue;

                var major = await db.Majors.FirstOrDefaultAsync(m => m.Code == e.Code);
                if (major == null)
                {
                    major = new Major { Code = e.Code, CreatedAt = DateTime.UtcNow };
                    db.Majors.Add(major);
                }

                // Cập nhật slug trong AdmissionMajors (bảng tra cứu điểm chuẩn) để khớp
                // với slug mới trong Majors. Hai bảng không có khóa ngoại nên phải tự đồng bộ.
                var admissionMajor = await db.AdmissionMajors.FirstOrDefaultAsync(m => m.MajorCode == e.Code);
                if (admissionMajor != null)
                {
                    admissionMajor.MajorName = e.Name;
                    admissionMajor.Slug = e.Slug;
                    admissionMajor.ImageUrl = e.ImageUrl;
                }

                major.Name = e.Name;
                major.Slug = e.Slug;
                major.DepartmentId = dept.Id;
                major.ImageUrl = e.ImageUrl;
                major.SourceUrl = e.SourceUrl;
                major.Description = Join(e.IntroParagraphs);
                major.SkillsDescription = Join(e.SkillsParagraphs);
                major.CareerDescription = Join(e.CareerParagraphs);
                major.DurationDescription = Join(e.DurationParagraphs);

                // Học phi không lưu ở đây. Mức học phí đổi theo từng năm tuyển sinh
                // nên một cột "đồng/năm" trên mọi ngành sẽ đọc như một bảng giá
                // cố định cho mọi năm — đúng điều trang /hoc-phi cần tránh. Các số
                // 13.000.000–15.000.000 đồng từ bản seed mẫu trước đây là số tự
                // đặt, không có văn bản nào căn cứ, nên bị xoá ở đây.
                major.Tuition = 0;

                if (string.IsNullOrWhiteSpace(major.DegreeLevel))
                    major.DegreeLevel = "Đại học";
            }

            await db.SaveChangesAsync();

            // ---- 3d. Đồng bộ thêm tin tức và sự kiện thật của Trường ----
            // Chạy mỗi lần khởi động: nếu một bài chưa có (theo slug) thì thêm vào.
            // Dữ liệu trích từ cổng thông tin chính thức của Trường (viu.edu.vn),
            // không tự bịa. Quản trị có thể chỉnh sửa/xoá trong /admin sau đó.
            var newsCatTuyenSinh = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "tuyen-sinh");
            var newsCatHoatDong = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "hoat-dong-nha-truong");
            if (newsCatTuyenSinh == null)
            {
                newsCatTuyenSinh = new NewsCategory { Name = "Tuyển sinh", Slug = "tuyen-sinh" };
                db.NewsCategories.Add(newsCatTuyenSinh);
                await db.SaveChangesAsync();
            }
            if (newsCatHoatDong == null)
            {
                newsCatHoatDong = new NewsCategory { Name = "Hoạt động nhà trường", Slug = "hoat-dong-nha-truong" };
                db.NewsCategories.Add(newsCatHoatDong);
                await db.SaveChangesAsync();
            }

            var moreNews = new[]
            {
                new News
                {
                    Title = "Đón tiếp Đoàn đại biểu Trường Đại học Văn hóa Trung Quốc (CCU - Đài Loan)",
                    Slug = SlugHelper.GenerateSlug("Đón tiếp Đoàn đại biểu Trường Đại học Văn hóa Trung Quốc (CCU - Đài Loan)"),
                    Summary = "Trường Đại học Công nghiệp Việt - Hung đón tiếp Đoàn đại biểu Trường Đại học Văn hóa Trung Quốc (CCU - Đài Loan) đến thăm, làm việc và ký kết Biên bản ghi nhớ hợp tác.",
                    Content = "<p>Trường Đại học Công nghiệp Việt - Hung đã đón tiếp Đoàn đại biểu Trường Đại học Văn hóa Trung Quốc (CCU - Đài Loan) đến thăm, làm việc và ký kết Biên bản ghi nhớ hợp tác giữa hai bên.</p><p>Việc ký kết mở ra cơ hội hợp tác trong đào tạo, nghiên cứu khoa học và trao đổi sinh viên giữa hai trường.</p>",
                    Thumbnail = "/images/viu/viu-signing.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Mở rộng cơ hội du học Hàn Quốc: KVN Group hợp tác cùng Trường ĐHCN Việt - Hung",
                    Slug = SlugHelper.GenerateSlug("Mở rộng cơ hội du học Hàn Quốc: KVN Group hợp tác cùng Trường ĐHCN Việt - Hung"),
                    Summary = "Trường Đại học Công nghiệp Việt - Hung ký kết hợp tác với KVN Group nhằm mở rộng cơ hội du học Hàn Quốc cho sinh viên.",
                    Content = "<p>Trường Đại học Công nghiệp Việt - Hung và KVN Group đã ký kết biên bản hợp tác nhằm mở rộng cơ hội du học Hàn Quốc cho sinh viên nhà trường.</p><p>Sinh viên có cơ hội trải nghiệm môi trường học tập quốc tế và nâng cao năng lực ngoại ngữ.</p>",
                    Thumbnail = "/images/viu/viu-partnership.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Hành trình 4 năm nhóm ngành Kinh tế – Quản lý K46",
                    Slug = SlugHelper.GenerateSlug("Hành trình 4 năm nhóm ngành Kinh tế – Quản lý K46"),
                    Summary = "Nhìn lại hành trình 4 năm học tập và rèn luyện của sinh viên nhóm ngành Kinh tế – Quản lý khóa K46.",
                    Content = "<p>Sinh viên nhóm ngành Kinh tế – Quản lý khóa K46 đã hoàn thành chương trình đào tạo và chuẩn bị tốt nghiệp.</p><p>Trong 4 năm học, sinh viên đã được trang bị kiến thức chuyên môn, kỹ năng thực hành và kinh nghiệm thực tập tại doanh nghiệp.</p>",
                    Thumbnail = "/images/viu/viu-khoa-kinhte.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Hành trình 5 năm – Dấu ấn của nhóm ngành Kỹ thuật – Công nghệ",
                    Slug = SlugHelper.GenerateSlug("Hành trình 5 năm – Dấu ấn của nhóm ngành Kỹ thuật – Công nghệ"),
                    Summary = "Dấu ấn 5 năm học tập và rèn luyện của sinh viên nhóm ngành Kỹ thuật – Công nghệ.",
                    Content = "<p>Sinh viên nhóm ngành Kỹ thuật – Công nghệ đã hoàn thành chương trình đào tạo hệ kỹ sư và chuẩn bị tốt nghiệp.</p><p>Trong 5 năm, sinh viên được đào tạo bài bản từ lý thuyết đến thực hành tại các xưởng, phòng thí nghiệm và doanh nghiệp đối tác.</p>",
                    Thumbnail = "/images/viu/viu-khoa-dien.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Nghiệm thu đề tài khoa học và công nghệ cấp trường",
                    Slug = SlugHelper.GenerateSlug("Nghiệm thu đề tài khoa học và công nghệ cấp trường"),
                    Summary = "Hội đồng nghiệm thu Trường Đại học Công nghiệp Việt - Hung nghiệm thu đề tài khoa học và công nghệ cấp trường.",
                    Content = "<p>Hội đồng nghiệm thu của Trường đã nghiệm thu đề tài khoa học và công nghệ cấp trường với kết quả đạt yêu cầu.</p><p>Đề tài góp phần giải quyết các vấn đề thực tiễn trong sản xuất và đào tạo của nhà trường.</p>",
                    Thumbnail = "/images/viu/viu-khoa-cntt.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Tăng cường hợp tác đào tạo Nghiên cứu sinh với Đại học Khoa học Kỹ thuật Điện tử Quế Lâm - Trung Quốc",
                    Slug = SlugHelper.GenerateSlug("Tăng cường hợp tác đào tạo Nghiên cứu sinh với Đại học Khoa học Kỹ thuật Điện tử Quế Lâm - Trung Quốc"),
                    Summary = "Trường ĐHCN Việt - Hung tăng cường hợp tác đào tạo nghiên cứu sinh với Đại học Khoa học Kỹ thuật Điện tử Quế Lâm (Trung Quốc).",
                    Content = "<p>Trường Đại học Công nghiệp Việt - Hung và Đại học Khoa học Kỹ thuật Điện tử Quế Lâm (Trung Quốc) đã ký kết thỏa thuận hợp tác đào tạo nghiên cứu sinh.</p><p>Thỏa thuận mở ra cơ hội cho cán bộ, giảng viên và sinh viên nhà trường theo học chương trình tiến sĩ tại Trung Quốc.</p>",
                    Thumbnail = "/images/viu/viu-signing.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Công khai sáng kiến cơ sở năm 2026",
                    Slug = SlugHelper.GenerateSlug("Công khai sáng kiến cơ sở năm 2026"),
                    Summary = "Trường Đại học Công nghiệp Việt - Hung công khai danh sách sáng kiến cơ sở năm 2026.",
                    Content = "<p>Trường Đại học Công nghiệp Việt - Hung công khai danh sách các sáng kiến cơ sở đã được nghiệm thu trong năm 2026.</p><p>Danh sách được công khai theo quy định của Bộ Giáo dục và Đào tạo về công khai giáo dục.</p>",
                    Thumbnail = "/images/viu/viu-hall.jpg",
                    CategoryId = newsCatHoatDong.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Danh sách 11 phương thức xét tuyển đại học dự kiến năm 2027",
                    Slug = SlugHelper.GenerateSlug("Danh sách 11 phương thức xét tuyển đại học dự kiến năm 2027"),
                    Summary = "Bộ GD&ĐT công bố danh sách 11 phương thức xét tuyển đại học dự kiến áp dụng từ năm 2027.",
                    Content = "<p>Bộ Giáo dục và Đào tạo công bố danh sách 11 phương thức xét tuyển đại học dự kiến áp dụng từ năm 2027.</p><p>Trường Đại học Công nghiệp Việt - Hung sẽ cập nhật phương thức áp dụng theo thông báo chính thức của Bộ.</p>",
                    Thumbnail = "/images/viu/viu-opening.jpg",
                    CategoryId = newsCatTuyenSinh.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc)
                },
                new News
                {
                    Title = "Trường ĐHCN Việt - Hung thông báo điểm trúng tuyển đại học chính quy K50",
                    Slug = SlugHelper.GenerateSlug("Trường ĐHCN Việt - Hung thông báo điểm trúng tuyển đại học chính quy K50"),
                    Summary = "Trường Đại học Công nghiệp Việt - Hung công bố điểm trúng tuyển và danh sách thí sinh trúng tuyển đại học chính quy K50.",
                    Content = "<p>Trường Đại học Công nghiệp Việt - Hung thông báo điểm trúng tuyển và danh sách thí sinh trúng tuyển đại học chính quy khóa 50 sau lọc ảo của Bộ GD&ĐT.</p><p>Thí sinh trúng tuyển cần theo dõi thông báo nhập học trên cổng tuyển sinh của Trường.</p>",
                    Thumbnail = "/images/viu/viu-students-cheer.jpg",
                    CategoryId = newsCatTuyenSinh.Id,
                    IsPublished = true,
                    PublishedAt = new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc)
                }
            };

            foreach (var n in moreNews)
            {
                if (!await db.News.AnyAsync(x => x.Slug == n.Slug))
                {
                    db.News.Add(n);
                }
            }
            await db.SaveChangesAsync();

            // ---- 3e. Đồng bộ thêm sự kiện thật của Trường ----
            var moreEvents = new[]
            {
                new Event
                {
                    Title = "LEADERS TALK - TƯƠNG LAI CÓ CẦN BẠN KHÔNG?",
                    Slug = SlugHelper.GenerateSlug("LEADERS TALK - TƯƠNG LAI CÓ CẦN BẠN KHÔNG"),
                    Description = "<p>Chương trình LEADERS TALK với chủ đề \"Tương lai có cần bạn không?\" - xây dựng thương hiệu cá nhân và network khi còn là sinh viên.</p><p>Sinh viên được giao lưu với các diễn giả doanh nghiệp, học kỹ năng xây dựng thương hiệu cá nhân và mở rộng mạng lưới quan hệ.</p>",
                    ImageUrl = "/images/viu/viu-stage.jpg",
                    Location = "Hội trường nhà trường",
                    StartAt = new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc),
                    IsPublished = true
                },
                new Event
                {
                    Title = "THÔNG BÁO XÉT TUYỂN ĐỢT 2 - BỔ SUNG ĐẠI HỌC CHÍNH QUY K50",
                    Slug = SlugHelper.GenerateSlug("THÔNG BÁO XÉT TUYỂN ĐỢT 2 - BỔ SUNG ĐẠI HỌC CHÍNH QUY K50"),
                    Description = "<p>Trường Đại học Công nghiệp Việt - Hung thông báo xét tuyển đợt 2 - bổ sung đại học chính quy khóa 50.</p><p>Thí sinh chưa trúng tuyển có thể nộp hồ sơ xét tuyển bổ sung theo thông báo.</p>",
                    ImageUrl = "/images/viu/viu-opening.jpg",
                    Location = "Ban Tuyển sinh",
                    StartAt = new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc),
                    IsPublished = true
                },
                new Event
                {
                    Title = "Thông báo tuyển dụng viên chức năm 2026",
                    Slug = SlugHelper.GenerateSlug("Thông báo tuyển dụng viên chức năm 2026"),
                    Description = "<p>Trường Đại học Công nghiệp Việt - Hung thông báo tuyển dụng viên chức năm 2026.</p><p>Ứng viên đủ điều kiện nộp hồ sơ theo hướng dẫn trong thông báo.</p>",
                    ImageUrl = "/images/viu/viu-hall.jpg",
                    Location = "Phòng Tổ chức - Hành chính",
                    StartAt = new DateTime(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc),
                    IsPublished = true
                },
                new Event
                {
                    Title = "HỘI NGHỊ CÔNG BỐ QUYẾT ĐỊNH VỀ CÔNG TÁC CÁN BỘ",
                    Slug = SlugHelper.GenerateSlug("HỘI NGHỊ CÔNG BỐ QUYẾT ĐỊNH VỀ CÔNG TÁC CÁN BỘ"),
                    Description = "<p>Hội nghị công bố quyết định về công tác cán bộ - công bố quyết định giao phụ trách Trường Đại học Công nghiệp Việt - Hung.</p>",
                    ImageUrl = "/images/viu/viu-signing.jpg",
                    Location = "Hội trường nhà trường",
                    StartAt = new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc),
                    IsPublished = true
                }
            };

            foreach (var ev in moreEvents)
            {
                if (!await db.Events.AnyAsync(x => x.Slug == ev.Slug))
                {
                    db.Events.Add(ev);
                }
            }
            await db.SaveChangesAsync();

            // ---- 4. News categories + sample news ----
            if (!await db.NewsCategories.AnyAsync())
            {
                var catTuyenSinh = new NewsCategory { Name = "Tuyển sinh", Slug = "tuyen-sinh" };
                var catHoatDong = new NewsCategory { Name = "Hoạt động nhà trường", Slug = "hoat-dong-nha-truong" };
                db.NewsCategories.AddRange(catTuyenSinh, catHoatDong);
                await db.SaveChangesAsync();

                db.News.AddRange(
                    new News
                    {
                        Title = "VIU công bố phương án tuyển sinh năm 2026",
                        Slug = SlugHelper.GenerateSlug("VIU công bố phương án tuyển sinh năm 2026"),
                        Summary = "Trường Đại học Công nghiệp Việt - Hung công bố các phương thức xét tuyển năm 2026.",
                        Content = "<p>Nội dung chi tiết về phương án tuyển sinh sẽ được cập nhật bởi Ban Tuyển sinh...</p>",
                        Thumbnail = "/images/real/khoa_cntt.jpg",
                        CategoryId = catTuyenSinh.Id,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new News
                    {
                        Title = "Sinh viên VIU đạt giải cao tại cuộc thi lập trình",
                        Slug = SlugHelper.GenerateSlug("Sinh viên VIU đạt giải cao tại cuộc thi lập trình"),
                        Summary = "Đội tuyển Khoa CNTT xuất sắc giành giải Nhì cấp thành phố.",
                        Content = "<p>Chúc mừng đội tuyển sinh viên đã nỗ lực tập luyện và đạt thành tích cao...</p>",
                        Thumbnail = "/images/real/cntt.jpg",
                        CategoryId = catHoatDong.Id,
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-5)
                    }
                );
                await db.SaveChangesAsync();
            }

            // ---- 5. Announcements ----
            if (!await db.AnnouncementCategories.AnyAsync())
            {
                var catDaoTao = new AnnouncementCategory { Name = "Đào tạo" };
                db.AnnouncementCategories.Add(catDaoTao);
                await db.SaveChangesAsync();

                db.Announcements.Add(new Announcement
                {
                    Title = "Thông báo lịch nghỉ Tết Nguyên đán 2026",
                    Content = "<p>Nhà trường thông báo lịch nghỉ Tết Nguyên đán tới toàn thể cán bộ, giảng viên, sinh viên...</p>",
                    CategoryId = catDaoTao.Id,
                    IsImportant = true,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-1)
                });
                await db.SaveChangesAsync();
            }

            // ---- 6. Events ----
            if (!await db.Events.AnyAsync())
            {
                db.Events.Add(new Event
                {
                    Title = "Ngày hội việc làm VIU 2026",
                    Slug = SlugHelper.GenerateSlug("Ngày hội việc làm VIU 2026"),
                    Description = "Kết nối sinh viên với hơn 50 doanh nghiệp đối tác.",
                    Location = "Cơ sở Thanh Xuân",
                    ImageUrl = "/images/real/hoitn.jpg",
                    StartAt = DateTime.UtcNow.AddDays(20),
                    IsPublished = true
                });
                await db.SaveChangesAsync();
            }

            // ---- 7. Campuses (real VIU locations) ----
            if (!await db.Campuses.AnyAsync())
            {
                db.Campuses.AddRange(
                    new Campus { Name = "Cơ sở Sơn Tây", Address = "Số 16 Hữu Nghị, phường Tùng Thiện, TP. Hà Nội", DisplayOrder = 1 },
                    new Campus { Name = "Cơ sở Thạch Thất", Address = "Số 88, đường 419 - Tây Phương, TP. Hà Nội", DisplayOrder = 2 },
                    new Campus { Name = "Cơ sở Thanh Xuân", Address = "Số 27 Lê Văn Lương, phường Thanh Xuân, TP. Hà Nội", DisplayOrder = 3 }
                );
                await db.SaveChangesAsync();
            }

            // ---- 8. Banners ----
            if (!await db.Banners.AnyAsync())
            {
                db.Banners.AddRange(
                    new Banner
                    {
                        Title = "Tuyển sinh đại học năm 2026",
                        Description = "Đào tạo gắn liền thực tiễn với nhu cầu lao động. Xem đề án tuyển sinh và tra cứu điểm chuẩn các ngành.",
                        ImageUrl = "/images/real/banner3.jpg",
                        ButtonText = "Xem đề án tuyển sinh",
                        ButtonUrl = "/tuyen-sinh",
                        DisplayOrder = 1
                    },
                    new Banner
                    {
                        Title = "Đào tạo thực hành, gắn liền doanh nghiệp",
                        Description = "Chương trình đào tạo được thiết kế cùng các cơ sở trong nước và Hungary.",
                        ImageUrl = "/images/real/baner2.jpg",
                        ButtonText = "Xem chương trình đào tạo",
                        ButtonUrl = "/dao-tao",
                        DisplayOrder = 2
                    },
                    new Banner
                    {
                        Title = "Hỗ trợ học tập và sinh hoạt sinh viên",
                        Description = "Thông tin ký túc xá, học bổng và hoạt động đoàn thể tại trường.",
                        ImageUrl = "/images/departments/khoa-cntt-hoat-dong.jpg",
                        ButtonText = "Đời sống sinh viên",
                        ButtonUrl = "/sinh-vien",
                        DisplayOrder = 3
                    }
                );
                await db.SaveChangesAsync();
            }

            // ---- 9. Subjects ----
            if (!await db.Subjects.AnyAsync())
            {
                var cnttMajor = await db.Majors.FirstOrDefaultAsync(m => m.Code == "7480201");
                if (cnttMajor != null)
                {
                    db.Subjects.AddRange(
                        new Subject { Code = "IT101", Name = "Lập trình C/C++ cơ bản", Credits = 3, MajorId = cnttMajor.Id, Description = "Nắm vững cú pháp cơ bản, con trỏ và cấu trúc dữ liệu cơ bản." },
                        new Subject { Code = "IT201", Name = "Lập trình hướng đối tượng (C# / Java)", Credits = 4, MajorId = cnttMajor.Id, Description = "Các nguyên lý OOP: đóng gói, kế thừa, đa hình, trừu tượng." },
                        new Subject { Code = "IT202", Name = "Cấu trúc dữ liệu & Giải thuật", Credits = 4, MajorId = cnttMajor.Id, Description = "Danh sách liên kết, cây, đồ thị, thuật toán sắp xếp và tìm kiếm." },
                        new Subject { Code = "IT301", Name = "Hệ quản trị cơ sở dữ liệu", Credits = 3, MajorId = cnttMajor.Id, Description = "Thiết kế CSDL quan hệ, SQL nâng cao, chỉ mục và tối ưu hóa." },
                        new Subject { Code = "IT305", Name = "Phát triển ứng dụng Web (.NET / React)", Credits = 4, MajorId = cnttMajor.Id, Description = "Xây dựng Web fullstack hiện đại với ASP.NET Core và React." },
                        new Subject { Code = "IT401", Name = "Trí tuệ nhân tạo & Machine Learning", Credits = 3, MajorId = cnttMajor.Id, Description = "Nhập môn AI, mạng nơ-ron học sâu và các ứng dụng thực tế." }
                    );
                }

                var dienMajor = await db.Majors.FirstOrDefaultAsync(m => m.Code == "7510301");
                if (dienMajor != null)
                {
                    db.Subjects.AddRange(
                        new Subject { Code = "EE101", Name = "Mạch điện 1", Credits = 3, MajorId = dienMajor.Id, Description = "Các định luật mạch cơ bản, mạch điện xoay chiều một pha và ba pha." },
                        new Subject { Code = "EE202", Name = "Kỹ thuật vi điều khiển và IoT", Credits = 4, MajorId = dienMajor.Id, Description = "Lập trình nhúng vi điều khiển ARM, ESP32 và ứng dụng Internet of Things." },
                        new Subject { Code = "EE303", Name = "Điều khiển logic khả trình (PLC)", Credits = 3, MajorId = dienMajor.Id, Description = "Tự động hóa nhà máy và dây chuyền sản xuất sử dụng PLC Siemens/Mitsubishi." }
                    );
                }

                var qtkdMajor = await db.Majors.FirstOrDefaultAsync(m => m.Code == "7340101");
                if (qtkdMajor != null)
                {
                    db.Subjects.AddRange(
                        new Subject { Code = "BA101", Name = "Quản trị học", Credits = 3, MajorId = qtkdMajor.Id, Description = "Các chức năng quản trị: lập kế hoạch, tổ chức, lãnh đạo, kiểm soát." },
                        new Subject { Code = "BA201", Name = "Marketing căn bản", Credits = 3, MajorId = qtkdMajor.Id, Description = "Nghiên cứu thị trường, chiến lược 4P, hành vi người tiêu dùng." },
                        new Subject { Code = "BA302", Name = "Quản trị tài chính doanh nghiệp", Credits = 4, MajorId = qtkdMajor.Id, Description = "Phân tích báo cáo tài chính, quản lý dòng tiền và danh mục đầu tư." }
                    );
                }

                await db.SaveChangesAsync();
            }

            // ---- 10. Lecturers ----
            if (!await db.Lecturers.AnyAsync())
            {
                var cntt = await db.Departments.FirstOrDefaultAsync(d => d.Code == "CNTT");
                var dien = await db.Departments.FirstOrDefaultAsync(d => d.Code == "DIEN");
                var kt = await db.Departments.FirstOrDefaultAsync(d => d.Code == "KT");

                if (cntt != null)
                {
                    db.Lecturers.AddRange(
                        new Lecturer
                        {
                            FullName = "PGS.TS. Nguyễn Văn Hùng",
                            AcademicTitle = "Phó Giáo sư, Tiến sĩ",
                            DepartmentId = cntt.Id,
                            Email = "hungnv@viu.edu.vn",
                            Phone = "0912.345.678",
                            Biography = "Trưởng khoa CNTT, hơn 20 năm kinh nghiệm nghiên cứu trong lĩnh vực Trí tuệ nhân tạo và Khoa học dữ liệu."
                        },
                        new Lecturer
                        {
                            FullName = "ThS. Trần Thị Mai",
                            AcademicTitle = "Thạc sĩ",
                            DepartmentId = cntt.Id,
                            Email = "maitt@viu.edu.vn",
                            Phone = "0983.456.789",
                            Biography = "Phó Trưởng bộ môn Công nghệ phần mềm, chuyên gia kiến trúc hệ thống và Cloud Computing."
                        }
                    );
                }

                if (dien != null)
                {
                    db.Lecturers.Add(new Lecturer
                    {
                        FullName = "TS. Lê Hoàng Nam",
                        AcademicTitle = "Tiến sĩ",
                        DepartmentId = dien.Id,
                        Email = "namlh@viu.edu.vn",
                        Phone = "0904.123.456",
                        Biography = "Trưởng khoa Điện - Điện tử, chuyên gia Tự động hóa và Năng lượng tái tạo."
                    });
                }

                if (kt != null)
                {
                    db.Lecturers.Add(new Lecturer
                    {
                        FullName = "TS. Phạm Minh Tuấn",
                        AcademicTitle = "Tiến sĩ",
                        DepartmentId = kt.Id,
                        Email = "tuanpm@viu.edu.vn",
                        Phone = "0977.888.999",
                        Biography = "Trưởng khoa Kinh tế, chuyên gia Quản trị chiến lược và Tài chính quốc tế."
                    });
                }

                await db.SaveChangesAsync();
            }

            // ---- 11. Admission Methods ----
            if (!await db.AdmissionMethods.AnyAsync())
            {
                db.AdmissionMethods.AddRange(
                    new AdmissionMethod
                    {
                        MethodCode = "100",
                        Name = "Xét tuyển theo kết quả thi tốt nghiệp THPT",
                        Description = "Sử dụng tổng điểm 3 môn thi tốt nghiệp THPT thuộc tổ hợp xét tuyển tương ứng của từng ngành.",
                        Requirements = "Thí sinh có tổng điểm 3 môn trong tổ hợp xét tuyển đạt ngưỡng đảm bảo chất lượng đầu vào của VIU.",
                        DisplayOrder = 1
                    },
                    new AdmissionMethod
                    {
                        MethodCode = "200",
                        Name = "Xét tuyển theo kết quả học bạ THPT",
                        Description = "Xét điểm trung bình học tập 3 năm THPT (lớp 10, lớp 11 và học kỳ 1 lớp 12) hoặc tổng điểm 3 môn tổ hợp lớp 12.",
                        Requirements = "Tổng điểm trung bình các môn xét tuyển đạt từ 18.0 điểm trở lên.",
                        DisplayOrder = 2
                    },
                    new AdmissionMethod
                    {
                        MethodCode = "301",
                        Name = "Xét tuyển thẳng theo quy chế của Bộ GD&ĐT",
                        Description = "Dành cho thí sinh đạt giải học sinh giỏi quốc gia, quốc tế hoặc đạt thành tích xuất sắc trong các cuộc thi KHKT.",
                        Requirements = "Đạt giải Nhất, Nhì, Ba cấp quốc gia hoặc thuộc diện tuyển thẳng theo thông tư của Bộ GD&ĐT.",
                        DisplayOrder = 3
                    },
                    new AdmissionMethod
                    {
                        MethodCode = "402",
                        Name = "Xét tuyển kết hợp chứng chỉ ngoại ngữ quốc tế",
                        Description = "Dành cho thí sinh có chứng chỉ IELTS từ 5.0 trở lên (hoặc TOEFL/TOEIC tương đương) kết hợp điểm học bạ.",
                        Requirements = "Chứng chỉ còn thời hạn tính đến ngày xét tuyển, kết hợp điểm 2 môn còn lại trong tổ hợp >= 12.0 điểm.",
                        DisplayOrder = 4
                    }
                );
                await db.SaveChangesAsync();
            }

            // ---- 12. Admission Majors & Scores ----
            if (!await db.AdmissionMajors.AnyAsync())
            {
                db.AdmissionMajors.AddRange(
                    new AdmissionMajor
                    {
                        MajorCode = "7480201",
                        MajorName = "Công nghệ thông tin",
                        Quota = 350,
                        SubjectCombinations = "A00, A01, D01, D07",
                        Score2023 = 19.5,
                        Score2024 = 20.5,
                        Score2025 = 21.0,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Ngành trọng điểm, nhu cầu nhân lực rất cao."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7480101",
                        MajorName = "Khoa học máy tính",
                        Quota = 200,
                        SubjectCombinations = "A00, A01, D01",
                        Score2023 = 19.0,
                        Score2024 = 20.0,
                        Score2025 = 20.5,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Đào tạo lập trình thực chiến, thực tập doanh nghiệp từ năm 3."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7510301",
                        MajorName = "Công nghệ kỹ thuật điện, điện tử",
                        Quota = 250,
                        SubjectCombinations = "A00, A01, B00, D01",
                        Score2023 = 18.0,
                        Score2024 = 18.5,
                        Score2025 = 19.0,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Hợp tác đào tạo với các tập đoàn công nghệ lớn."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7340101",
                        MajorName = "Quản trị kinh doanh",
                        Quota = 300,
                        SubjectCombinations = "A00, A01, D01, D07",
                        Score2023 = 18.5,
                        Score2024 = 19.0,
                        Score2025 = 19.5,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Đào tạo kỹ năng quản trị, khởi nghiệp, marketing số."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7340301",
                        MajorName = "Kế toán",
                        Quota = 200,
                        SubjectCombinations = "A00, A01, D01",
                        Score2023 = 18.0,
                        Score2024 = 18.5,
                        Score2025 = 19.0,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Tích hợp chứng chỉ nghề nghiệp kế toán quốc tế."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7520103",
                        MajorName = "Kỹ thuật cơ khí",
                        Quota = 180,
                        SubjectCombinations = "A00, A01, B00",
                        Score2023 = 17.5,
                        Score2024 = 18.0,
                        Score2025 = 18.5,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Cơ hội thực tập và làm việc tại Nhật Bản, Hàn Quốc."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7220201",
                        MajorName = "Ngôn ngữ Anh",
                        Quota = 150,
                        SubjectCombinations = "D01, D07, D09, D10",
                        Score2023 = 19.0,
                        Score2024 = 19.5,
                        Score2025 = 20.0,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Chuyên ngành Tiếng Anh thương mại và Công nghệ."
                    }
                );
                await db.SaveChangesAsync();
            }
            // Normalise legacy campus names/addresses to the official VIU locations.
            // The previous version mutated these entities but never saved, so the fix was
            // silently a no-op on every single startup.
            var campusesToFix = await db.Campuses.Where(c => !c.IsDeleted).ToListAsync();
            var campusesChanged = false;

            foreach (var c in campusesToFix)
            {
                var newName = c.Name;
                var newAddress = c.Address;

                if (c.Name.Contains("Tùng Thiện") || c.Name.Contains("Sơn Tây"))
                {
                    newName = "Cơ sở Sơn Tây";
                    newAddress = "Số 16 Hữu Nghị, phường Tùng Thiện, TP. Hà Nội";
                }
                else if (c.Name.Contains("Tây Phương") || c.Name.Contains("Thạch Thất"))
                {
                    newName = "Cơ sở Thạch Thất";
                    newAddress = "Số 88, đường 419 - Tây Phương, TP. Hà Nội";
                }
                else if (c.Name.Contains("Thanh Xuân"))
                {
                    newName = "Cơ sở Thanh Xuân";
                    newAddress = "Số 27 Lê Văn Lương, phường Thanh Xuân, TP. Hà Nội";
                }

                if (newName != c.Name || newAddress != c.Address)
                {
                    c.Name = newName;
                    c.Address = newAddress;
                    campusesChanged = true;
                }
            }

            if (campusesChanged)
                await db.SaveChangesAsync();
            // ---- 13. Gallery Images (Hình ảnh khuôn viên, phòng thực hành, sinh viên) ----
            if (!await db.GalleryImages.AnyAsync())
            {
                db.GalleryImages.AddRange(
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/banner1.jpeg",
                        Caption = "Khuôn viên Trụ sở chính Sơn Tây - Giảng đường trung tâm",
                        Album = "campus",
                        DisplayOrder = 1
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/baner2.jpg",
                        Caption = "Cơ sở Thạch Thất (Tây Phương) - Khu đào tạo công nghệ cao",
                        Album = "campus",
                        DisplayOrder = 2
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/banner3.jpg",
                        Caption = "Cơ sở Thanh Xuân - 27 Lê Văn Lương, Hà Nội",
                        Album = "campus",
                        DisplayOrder = 3
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/cntt.jpg",
                        Caption = "Phòng thực hành máy tính & Kỹ thuật phần mềm hiện đại",
                        Album = "facilities",
                        DisplayOrder = 4
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/khoa_dien.jpeg",
                        Caption = "Xưởng thực hành Điện - Tự động hóa & Robot công nghiệp",
                        Album = "facilities",
                        DisplayOrder = 5
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/khoa-cokhi.jpg",
                        Caption = "Xưởng thực hành Công nghệ Kỹ thuật Cơ khí",
                        Album = "facilities",
                        DisplayOrder = 6
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/hoitn.jpg",
                        Caption = "Lễ tốt nghiệp và trao bằng cử nhân, kỹ sư VIU",
                        Album = "events",
                        DisplayOrder = 7
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/anh_ngoai_khoa.jpg",
                        Caption = "Hoạt động sinh hoạt ngoại khóa và phong trào đoàn viên thanh niên",
                        Album = "students",
                        DisplayOrder = 8
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/real/sinhvientinhnguyen.jpg",
                        Caption = "Đội hình sinh viên tình nguyện VIU tiếp sức mùa thi",
                        Album = "students",
                        DisplayOrder = 9
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/departments/khoa-cntt.jpg",
                        Caption = "Cổng Khoa Công nghệ thông tin",
                        Album = "facilities",
                        DisplayOrder = 10
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/departments/khoa-cntt-hoat-dong.jpg",
                        Caption = "Sinh viên Khoa Công nghệ thông tin thực hành tại phòng máy",
                        Album = "facilities",
                        DisplayOrder = 11
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/departments/khoa-dien-dien-tu.jpg",
                        Caption = "Thực hành đấu nối tủ điện tử - Khoa Điện - Điện tử",
                        Album = "facilities",
                        DisplayOrder = 12
                    },
                    new GalleryImage
                    {
                        ImageUrl = "/images/departments/khoa-kinhte.jpg",
                        Caption = "Sinh viên làm việc nhóm tại Khoa Kinh tế",
                        Album = "facilities",
                        DisplayOrder = 13
                    }
                );
                await db.SaveChangesAsync();
            }

            // ---- 14. Admission Leads (Thí sinh đăng ký mẫu để Admin quản lý) ----
            // Gated on an explicit seed marker rather than on row contents. The previous
            // check was AnyAsync(l => l.FullName != "Phua Vàng"), i.e. "seed only when every
            // existing lead is named Phua Vàng" - a hard-coded personal name that silently
            // duplicated the sample leads whenever they were all deleted from the Admin UI.
            if (!await HasSeedMarkerAsync(db, SeedMarkers.AdmissionLeads))
            {
                db.AdmissionLeads.AddRange(
                    new AdmissionLead
                    {
                        FullName = "Nguyễn Hoàng Nam",
                        PhoneNumber = "0987654321",
                        Email = "hoangnam.cntt@example.com",
                        Province = "Hà Nội",
                        HighSchool = "THPT Sơn Tây",
                        DesiredMajor = "Công nghệ thông tin",
                        PreferredMethod = "Xét tuyển theo kết quả học bạ THPT",
                        Message = "Em muốn tìm hiểu học phí và cơ hội học bổng của ngành CNTT ạ.",
                        IsProcessed = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new AdmissionLead
                    {
                        FullName = "Trần Thị Mai Phương",
                        PhoneNumber = "0912345678",
                        Email = "maiphuong2008@example.com",
                        Province = "Vĩnh Phúc",
                        HighSchool = "THPT Vĩnh Yên",
                        DesiredMajor = "Quản trị kinh doanh",
                        PreferredMethod = "Xét tuyển theo kết quả thi tốt nghiệp THPT",
                        Message = "Cho em hỏi điểm chuẩn năm 2025 và thủ tục nộp hồ sơ xét tuyển sớm.",
                        IsProcessed = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-5)
                    },
                    new AdmissionLead
                    {
                        FullName = "Lê Quang Huy",
                        PhoneNumber = "0934567890",
                        Email = "quanghuy.ee@example.com",
                        Province = "Phú Thọ",
                        HighSchool = "THPT Hùng Vương",
                        DesiredMajor = "Công nghệ kỹ thuật điện, điện tử",
                        PreferredMethod = "Xét tuyển theo kết quả học bạ THPT",
                        Message = "Em đăng ký ký túc xá thì cần chuẩn bị giấy tờ gì ạ?",
                        IsProcessed = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new AdmissionLead
                    {
                        FullName = "Đỗ Bảo Trâm",
                        PhoneNumber = "0971234567",
                        Email = "baotram.se@example.com",
                        Province = "Hòa Bình",
                        HighSchool = "THPT Chuyên Hoàng Văn Thụ",
                        DesiredMajor = "Kỹ thuật phần mềm",
                        PreferredMethod = "Xét tuyển kết hợp chứng chỉ ngoại ngữ quốc tế",
                        Message = "Em có IELTS 6.0 có được ưu tiên xét tuyển thẳng vào Kỹ thuật phần mềm không?",
                        IsProcessed = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-8)
                    }
                );
                await db.SaveChangesAsync();
                await MarkSeededAsync(db, SeedMarkers.AdmissionLeads);
            }

            // ---- 15. Site settings (editable from Admin -> Cài đặt) ----
            // The chatbot previously hardcoded figures like "400 giảng viên" and
            // "quỹ học bổng hơn 5 tỷ" that nobody could verify or correct. They live in
            // the database now so an administrator owns the wording. Admin must edit these
            // before the site goes live — leave a value blank to hide the claim entirely.
            var defaultSettings = new List<SiteSetting>
            {
                new SiteSetting { Key = "contact.hotline", Group = "Liên hệ", Value = "024.3383.8345",
                    Description = "Số hotline tuyển sinh hiển thị trên chatbot và trang liên hệ." },
                new SiteSetting { Key = "contact.email", Group = "Liên hệ", Value = "tuyensinh@viu.edu.vn",
                    Description = "Email tư vấn tuyển sinh." },
                new SiteSetting { Key = "contact.address", Group = "Liên hệ", Value = "Số 16 Hữu Nghị, phường Tùng Thiện, thị xã Sơn Tây, TP. Hà Nội",
                    Description = "Địa chỉ trụ sở chính." },
                new SiteSetting { Key = "admission.scholarship.note", Group = "Tuyển sinh", Value = "",
                    Description = "Ghi chú về học bổng/học phí hiển thị cạnh bảng học phí. Để trống nếu chưa xác minh số liệu." },
                new SiteSetting { Key = "dorm.description", Group = "Ký túc xá", Value = "",
                    Description = "Mô tả ký túc xá cho chatbot. Để trống nếu chưa xác minh." },
                new SiteSetting { Key = "stats.lecturerTotal", Group = "Thống kê", Value = "",
                    Description = "Tổng số giảng viên. Để trống để chatbot không công bố con số này." },
                new SiteSetting { Key = "stats.studentTotal", Group = "Thống kê", Value = "0",
                    Description = "Tổng số sinh viên hiển thị ở trang chủ. Cập nhật sau khi có dữ liệu sinh viên thật." },
                new SiteSetting { Key = "stats.partnerTotal", Group = "Thống kê", Value = "",
                    Description = "Số doanh nghiệp đối tác. Để trống để ẩn." },
                new SiteSetting { Key = "stats.employmentRate", Group = "Thống kê", Value = "",
                    Description = "Tỷ lệ sinh viên có việc làm sau 12 tháng. Để trống để ẩn." }
            };

            var missingSettingKeys = defaultSettings.Select(s => s.Key).ToList();
            var existingSettingKeys = await db.SiteSettings
                .Where(s => missingSettingKeys.Contains(s.Key))
                .Select(s => s.Key)
                .ToListAsync();

            var newSettings = defaultSettings
                .Where(s => !existingSettingKeys.Contains(s.Key))
                .ToList();

            if (newSettings.Count > 0)
            {
                db.SiteSettings.AddRange(newSettings);
                await db.SaveChangesAsync();
            }
        }
    }
}
