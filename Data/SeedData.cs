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
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            if (db.Database.IsSqlite())
            {
                await db.Database.EnsureCreatedAsync();
            }
            else
            {
                await db.Database.MigrateAsync();
            }

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // ---- 1. Roles ----
            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---- 2. Admin account ----
            var adminEmail = config["SeedAdmin:Email"] ?? "admin@viu.edu.vn";
            var adminPassword = config["SeedAdmin:Password"] ?? "Admin@123456";

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = config["SeedAdmin:FullName"] ?? "Quản trị hệ thống"
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
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
                    Description = "Đào tạo nguồn nhân lực CNTT chất lượng cao.",
                    Email = "cntt@viu.edu.vn"
                };
                var dien = new Department
                {
                    Code = "DIEN",
                    Name = "Khoa Điện - Điện tử",
                    Slug = SlugHelper.GenerateSlug("Khoa Điện - Điện tử"),
                    Description = "Đào tạo kỹ sư điện, điện tử, tự động hóa.",
                    Email = "dien@viu.edu.vn"
                };
                var kinhte = new Department
                {
                    Code = "KT",
                    Name = "Khoa Kinh tế",
                    Slug = SlugHelper.GenerateSlug("Khoa Kinh tế"),
                    Description = "Đào tạo cử nhân kinh tế, quản trị kinh doanh.",
                    Email = "kinhte@viu.edu.vn"
                };
                db.Departments.AddRange(cntt, dien, kinhte);
                await db.SaveChangesAsync();

                var majors = new List<Major>
                {
                    new() {
                        Code = "7480201", Name = "Công nghệ thông tin",
                        Slug = SlugHelper.GenerateSlug("Công nghệ thông tin"),
                        DepartmentId = cntt.Id, DegreeLevel = "Đại học",
                        DurationYears = 4, Tuition = 15000000,
                        Description = "Chương trình đào tạo Công nghệ thông tin hiện đại, gắn liền thực hành.",
                        CareerDescription = "Lập trình viên, kỹ sư phần mềm, quản trị hệ thống, phân tích dữ liệu."
                    },
                    new() {
                        Code = "7480103", Name = "Kỹ thuật phần mềm",
                        Slug = SlugHelper.GenerateSlug("Kỹ thuật phần mềm"),
                        DepartmentId = cntt.Id, DegreeLevel = "Đại học",
                        DurationYears = 4, Tuition = 15000000,
                        Description = "Chuyên sâu về quy trình phát triển phần mềm chuyên nghiệp.",
                        CareerDescription = "Kỹ sư phát triển phần mềm, kiểm thử, quản lý dự án CNTT."
                    },
                    new() {
                        Code = "7510301", Name = "Công nghệ kỹ thuật điện, điện tử",
                        Slug = SlugHelper.GenerateSlug("Công nghệ kỹ thuật điện điện tử"),
                        DepartmentId = dien.Id, DegreeLevel = "Đại học",
                        DurationYears = 4, Tuition = 14000000,
                        Description = "Đào tạo kỹ sư thiết kế, vận hành hệ thống điện - điện tử.",
                        CareerDescription = "Kỹ sư điện, tự động hóa, bảo trì hệ thống công nghiệp."
                    },
                    new() {
                        Code = "7340101", Name = "Quản trị kinh doanh",
                        Slug = SlugHelper.GenerateSlug("Quản trị kinh doanh"),
                        DepartmentId = kinhte.Id, DegreeLevel = "Đại học",
                        DurationYears = 4, Tuition = 13000000,
                        Description = "Trang bị kiến thức quản trị doanh nghiệp toàn diện.",
                        CareerDescription = "Chuyên viên kinh doanh, marketing, quản lý doanh nghiệp."
                    }
                };
                db.Majors.AddRange(majors);
                await db.SaveChangesAsync();
            }

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
                    StartAt = DateTime.UtcNow.AddDays(20),
                    IsPublished = true
                });
                await db.SaveChangesAsync();
            }

            // ---- 7. Campuses (real VIU locations) ----
            if (!await db.Campuses.AnyAsync())
            {
                db.Campuses.AddRange(
                    new Campus { Name = "Cơ sở Tùng Thiện", Address = "Thị xã Sơn Tây, Hà Nội", DisplayOrder = 1 },
                    new Campus { Name = "Cơ sở Tây Phương", Address = "Huyện Thạch Thất, Hà Nội", DisplayOrder = 2 },
                    new Campus { Name = "Cơ sở Thanh Xuân", Address = "Quận Thanh Xuân, Hà Nội", DisplayOrder = 3 }
                );
                await db.SaveChangesAsync();
            }

            // ---- 8. Banners ----
            if (!await db.Banners.AnyAsync())
            {
                db.Banners.Add(new Banner
                {
                    Title = "Tuyển sinh 2026 - Kiến tạo tương lai công nghệ",
                    Description = "Đăng ký xét tuyển ngay hôm nay cùng VIU.",
                    ImageUrl = "/images/banner-placeholder.jpg",
                    ButtonText = "Xem chi tiết",
                    ButtonUrl = "/tuyen-sinh",
                    DisplayOrder = 1
                });
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
                        Score2023 = 19.5m,
                        Score2024 = 20.5m,
                        Score2025 = 21.0m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Ngành trọng điểm, nhu cầu nhân lực rất cao."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7480103",
                        MajorName = "Kỹ thuật phần mềm",
                        Quota = 200,
                        SubjectCombinations = "A00, A01, D01",
                        Score2023 = 19.0m,
                        Score2024 = 20.0m,
                        Score2025 = 20.5m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Đào tạo lập trình thực chiến, thực tập doanh nghiệp từ năm 3."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7510301",
                        MajorName = "Công nghệ kỹ thuật điện, điện tử",
                        Quota = 250,
                        SubjectCombinations = "A00, A01, B00, D01",
                        Score2023 = 18.0m,
                        Score2024 = 18.5m,
                        Score2025 = 19.0m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Hợp tác đào tạo với các tập đoàn công nghệ lớn."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7340101",
                        MajorName = "Quản trị kinh doanh",
                        Quota = 300,
                        SubjectCombinations = "A00, A01, D01, D07",
                        Score2023 = 18.5m,
                        Score2024 = 19.0m,
                        Score2025 = 19.5m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Đào tạo kỹ năng quản trị, khởi nghiệp, marketing số."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7340301",
                        MajorName = "Kế toán",
                        Quota = 200,
                        SubjectCombinations = "A00, A01, D01",
                        Score2023 = 18.0m,
                        Score2024 = 18.5m,
                        Score2025 = 19.0m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Tích hợp chứng chỉ nghề nghiệp kế toán quốc tế."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7520103",
                        MajorName = "Kỹ thuật cơ khí",
                        Quota = 180,
                        SubjectCombinations = "A00, A01, B00",
                        Score2023 = 17.5m,
                        Score2024 = 18.0m,
                        Score2025 = 18.5m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Cơ hội thực tập và làm việc tại Nhật Bản, Hàn Quốc."
                    },
                    new AdmissionMajor
                    {
                        MajorCode = "7220201",
                        MajorName = "Ngôn ngữ Anh",
                        Quota = 150,
                        SubjectCombinations = "D01, D07, D09, D10",
                        Score2023 = 19.0m,
                        Score2024 = 19.5m,
                        Score2025 = 20.0m,
                        DegreeLevel = "Đại học chính quy",
                        Note = "Chuyên ngành Tiếng Anh thương mại và Công nghệ."
                    }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
