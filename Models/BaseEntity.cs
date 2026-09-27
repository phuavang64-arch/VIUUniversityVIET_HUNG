namespace VIUUniversity.Models
{
    /// <summary>
    /// Common fields shared by every persisted entity in the system.
    /// Every table inherits CreatedAt / UpdatedAt / IsActive so nothing
    /// needs to reinvent auditing or soft-enable/disable logic.
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false; // soft delete flag
    }
}
