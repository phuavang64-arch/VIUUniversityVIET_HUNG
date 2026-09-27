using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VIUUniversity.Models
{
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

        [Required, MaxLength(270)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Summary { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Thumbnail { get; set; }

        public int CategoryId { get; set; }
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

        [Required, MaxLength(270)]
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
