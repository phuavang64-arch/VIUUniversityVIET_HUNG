using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VIUUniversity.Models
{
    /// <summary>A faculty/khoa. Owns Majors and Lecturers.</summary>
    public class Department : BaseEntity
    {
        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(220)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string? Image { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public ICollection<Major> Majors { get; set; } = new List<Major>();
        public ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();
    }

    /// <summary>A ngành đào tạo (degree program) belonging to a Department.</summary>
    public class Major : BaseEntity
    {
        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty; // e.g. 7480201

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(220)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? DegreeLevel { get; set; } // Đại học / Cao đẳng
        public int DurationYears { get; set; }

        /// <summary>
        /// Mô tả thời gian đào tạo nguyên văn của Trường, ví dụ "hệ cử nhân đại học
        /// là 4 năm, hệ kỹ sư từ 4,5 – 5 năm".
        ///
        /// Tồn tại song song với <see cref="DurationYears"/> vì Trường công bố thời
        /// gian khác nhau theo hệ (cử nhân / kỹ sư) và theo từng khóa; ép vào một
        /// số nguyên sẽ bịa mất phần thông tin đó.
        /// </summary>
        public string? DurationDescription { get; set; }

        /// <summary>Học phí (VND). double thay vì decimal — xem ghi chú ở AdmissionMajor.Score2023.</summary>
        public double Tuition { get; set; }
        public string? CareerDescription { get; set; }

        /// <summary>Kiến thức và kỹ năng ngành đào tạo trang bị cho sinh viên.</summary>
        public string? SkillsDescription { get; set; }

        /// <summary>
        /// Trang giới thiệu ngành trên cổng tuyển sinh chính thức của Trường.
        /// Mọi mã ngành trong seed đều trích từ đây — xem SeedData.MAJORS_K50.
        /// Giữ nguyên URL này để mọi ngày còn tra ngược nguồn.
        /// </summary>
        [MaxLength(300)]
        public string? SourceUrl { get; set; }

        public string? ImageUrl { get; set; }

        public int DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }

    /// <summary>A môn học belonging to a Major.</summary>
    public class Subject : BaseEntity
    {
        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int Credits { get; set; }
        public string? Description { get; set; }

        public int MajorId { get; set; }
        [ForeignKey(nameof(MajorId))]
        public Major? Major { get; set; }
    }

    /// <summary>Public-facing lecturer profile, linked 1-1 to an Identity user.</summary>
    public class Lecturer : BaseEntity
    {
        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public string? AcademicTitle { get; set; } // PGS.TS, ThS, ...
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
        public string? Biography { get; set; }

        public int DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }
    }

    /// <summary>A physical training campus (Tùng Thiện, Tây Phương, Thanh Xuân...).</summary>
    public class Campus : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>Business/university partner shown in the homepage logo strip.</summary>
    public class Partner : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? PartnerType { get; set; } // Doanh nghiệp / Trường quốc tế
        public string? Country { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>Homepage hero slider item, fully managed from the Admin CMS.</summary>
    public class Banner : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
        public string? ButtonText { get; set; }
        public string? ButtonUrl { get; set; }
        public int DisplayOrder { get; set; }
    }
}
