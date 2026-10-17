using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VIUUniversity.Models
{
    /// <summary>
    /// Chuyên mục của một bài <see cref="News"/>.
    ///
    /// Tin tức, Nghiên cứu khoa học, Hợp tác và Học phí dùng chung một bảng và
    /// chung một bộ CRUD trong /admin: khác nhau duy nhất ở chỗ hiển thị và ở vài
    /// trường riêng (số văn bản, file đính kèm — chỉ Học phí dùng). Tách thành
    /// bốn bảng riêng sẽ phải nhân đôn controller/service/view bốn lần.
    /// </summary>
    public static class ContentSection
    {
        public const string TinTuc = "TinTuc";
        public const string NghiênCứu = "NghiênCuu";
        public const string HopTac = "HopTac";
        public const string HocPhi = "HocPhi";

        public static readonly string[] All = { TinTuc, NghiênCứu, HopTac, HocPhi };

        /// <summary>Trả về giá trị hợp lệ; rác thì đổi về Tin tức.</summary>
        public static string Normalise(string? value)
            => All.Contains(value ?? "") ? value! : TinTuc;

        public static string Label(string? value) => value switch
        {
            NghiênCứu => "Nghiên cứu khoa học",
            HopTac => "Hợp tác",
            HocPhi => "Học phí",
            _ => "Tin tức",
        };
    }

    public class NewsCategory : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(170)]
        public string Slug { get; set; } = string.Empty;

        public ICollection<News> NewsItems { get; set; } = new List<News>();
    }

    public class News : BaseEntity
    {
        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        // No [Required] here: the slug is generated from the title when an editor leaves it
        // blank. Marking it required made that (and the controller's fallback branch)
        // unreachable, forcing every editor to hand-type a slug. The column stays NOT
        // NULL in the database because the property is a non-nullable string.
        [MaxLength(270)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Summary { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Thumbnail { get; set; }

        /// <summary>
        /// Xem <see cref="ContentSection"/>. Mặc định "TinTuc" để các bài có sẵn từ
        /// trước vẫn nằm đúng chuyên mục sau khi thêm cột.
        /// </summary>
        [MaxLength(20)]
        public string Section { get; set; } = ContentSection.TinTuc;

        // ---- Trường riêng cho chuyên mục Học phí ----
        // Học phí không trình bày như một bảng giá cố định cho mọi năm, nên mỗi
        // văn bản là một bài riêng và các trường dưới đây mô tả chính văn bản đó.
        // Để null để các chuyên mục khác không phải nhập gì.

        /// <summary>Số / ký hiệu văn bản, ví dụ "1234/TB-ĐHCN-VHD".</summary>
        [MaxLength(100)]
        public string? DocumentNumber { get; set; }

        /// <summary>Ngày ban hành của văn bản (khác với ngày đăng lên web).</summary>
        public DateTime? IssuedDate { get; set; }

        /// <summary>Năm học hoặc khóa tuyển sinh mà văn bản áp dụng, ví dụ "2026 - 2027".</summary>
        [MaxLength(50)]
        public string? AcademicYear { get; set; }

        /// <summary>Đường dẫn tệp PDF đính kèm trong wwwroot/uploads.</summary>
        [MaxLength(300)]
        public string? AttachmentUrl { get; set; }

        /// <summary>Tên tệp gốc để hiển thị cho người đọc.</summary>
        [MaxLength(200)]
        public string? AttachmentName { get; set; }

        /// <summary>
        /// Video nhúng đi kèm bài viết (YouTube / Vimeo / MP4 trực tiếp).
        ///
        /// Tách khỏi <see cref="Content"/> vì video không phải HTML do người dùng
        /// soạn: nó là một URL được nhúng bằng &lt;iframe&gt;, nên phải kiểm tra để chỉ
        /// nhận youtube.com / youtu.be / vimeo.com / .mp4. Nếu để chung trong Content thì
        /// một URL youtube bình thường sẽ bị HtmlSanitizer xoá thành rỗng.
        /// </summary>
        [MaxLength(300)]
        public string? VideoUrl { get; set; }

        /// <summary>
        /// Chuyên mục tin — KHÔNG bắt buộc.
        ///
        /// Tin tức thì luôn có chuyên mục, nhưng văn bản học phí hay thông báo hợp
        /// tác không phải bài tin nên không cần gắn vào danh mục nào. Cột bắt buộc
        /// buộc quản trị viên phải chọn một danh mục không liên quan, hoặc để 0 —
        /// mà 0 vi phạm ràng buộc khoá ngoại và làm cả trang lỗi 500.
        /// </summary>
        public int? CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public NewsCategory? Category { get; set; }

        public string? AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser? Author { get; set; }

        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }
        public int ViewCount { get; set; }
    }

    public class AnnouncementCategory : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
    }

    public class Announcement : BaseEntity
    {
        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public AnnouncementCategory? Category { get; set; }

        public string? AttachmentUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsImportant { get; set; }
        public bool IsPublished { get; set; }
    }

    public class Event : BaseEntity
    {
        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        // Generated from the title when left blank - see the note on News.Slug.
        [MaxLength(270)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Location { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public bool IsPublished { get; set; }
    }

    /// <summary>Records every meaningful write action for accountability.</summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;     // Create/Update/Delete/Login...
        public string EntityName { get; set; } = string.Empty; // "News", "Student"...
        public string? EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? IPAddress { get; set; }
    }
}
