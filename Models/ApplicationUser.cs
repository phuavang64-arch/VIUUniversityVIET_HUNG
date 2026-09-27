using Microsoft.AspNetCore.Identity;

namespace VIUUniversity.Models
{
    /// <summary>
    /// Extends ASP.NET Core Identity's default user with the profile fields
    /// the portal needs. Students and Lecturers each have a 1-1 record that
    /// points back to this via UserId.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Well-known role names used across the whole system.</summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Editor = "Editor";
        public const string Lecturer = "Lecturer";
        public const string Student = "Student";
        public const string Guest = "Guest";

        public static readonly string[] All = { Admin, Editor, Lecturer, Student, Guest };
    }
}
