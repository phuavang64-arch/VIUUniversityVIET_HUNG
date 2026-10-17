using System.ComponentModel.DataAnnotations;

namespace VIUUniversity.Models
{
    /// <summary>
    /// A single editable site-wide value (hotline number, scholarship budget, lecturer
    /// headcount...). These numbers were previously hardcoded in the chatbot script, which
    /// meant nobody could correct them and every claim went out unreviewed. Moving them
    /// into the database makes them editable from the Admin CMS and auditable.
    /// </summary>
    public class SiteSetting
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Value { get; set; }

        /// <summary>Plain-language hint shown next to the field in the Admin UI.</summary>
        [MaxLength(300)]
        public string? Description { get; set; }

        /// <summary>Groups related settings on the same form.</summary>
        [MaxLength(50)]
        public string? Group { get; set; }
    }
}