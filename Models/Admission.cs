using System.ComponentModel.DataAnnotations;

namespace VIUUniversity.Models
{
    /// <summary>
    /// Phương thức xét tuyển (Xét học bạ, Xét điểm thi tốt nghiệp, Tuyển thẳng...)
    /// </summary>
    public class AdmissionMethod : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MethodCode { get; set; }

        public string? Description { get; set; }

        public string? Requirements { get; set; }

        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Thông tin ngành tuyển sinh và điểm chuẩn qua các năm
    /// </summary>
    public class AdmissionMajor : BaseEntity
    {
        [Required, MaxLength(200)]
        public string MajorName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string MajorCode { get; set; } = string.Empty;

        public int? Quota { get; set; } // Chỉ tiêu tuyển sinh

        [MaxLength(200)]
        public string SubjectCombinations { get; set; } = string.Empty; // Ví dụ: A00, A01, D01, D07

        public decimal? Score2023 { get; set; }
        public decimal? Score2024 { get; set; }
        public decimal? Score2025 { get; set; }

        public string? DegreeLevel { get; set; } = "Đại học chính quy";
        public string? Note { get; set; }
    }

    /// <summary>
    /// Thông tin thí sinh đăng ký xét tuyển / tư vấn trực tuyến
    /// </summary>
    public class AdmissionLead : BaseEntity
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên"), MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress, MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Province { get; set; }

        [MaxLength(200)]
        public string? HighSchool { get; set; }

        [MaxLength(200)]
        public string? DesiredMajor { get; set; }

        [MaxLength(100)]
        public string? PreferredMethod { get; set; }

        public string? Message { get; set; }

        public bool IsProcessed { get; set; } = false;
    }

    /// <summary>
    /// Tin nhắn / Ý kiến đóng góp gửi qua trang Liên hệ
    /// </summary>
    public class ContactMessage : BaseEntity
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên"), MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone, MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề"), MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung"), MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
    }
}
