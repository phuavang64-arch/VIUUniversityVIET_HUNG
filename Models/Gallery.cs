using System.ComponentModel.DataAnnotations;

namespace VIUUniversity.Models
{
    /// <summary>Hình ảnh thư viện / gallery của trường, hiển thị trên trang chủ và trang giới thiệu.</summary>
    public class GalleryImage : BaseEntity
    {
        [Required, MaxLength(300)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Caption { get; set; }

        /// <summary>Album / nhóm ảnh: "campus", "events", "students", "facilities"...</summary>
        [MaxLength(100)]
        public string? Album { get; set; }

        public int DisplayOrder { get; set; }
    }
}
