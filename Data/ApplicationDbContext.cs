using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Models;

namespace VIUUniversity.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Major> Majors => Set<Major>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Lecturer> Lecturers => Set<Lecturer>();
        public DbSet<Campus> Campuses => Set<Campus>();
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<Banner> Banners => Set<Banner>();

        public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
        public DbSet<News> News => Set<News>();
        public DbSet<AnnouncementCategory> AnnouncementCategories => Set<AnnouncementCategory>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public DbSet<AdmissionMethod> AdmissionMethods => Set<AdmissionMethod>();
        public DbSet<AdmissionMajor> AdmissionMajors => Set<AdmissionMajor>();
        public DbSet<AdmissionLead> AdmissionLeads => Set<AdmissionLead>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---- Unique constraints & indexes ----
            builder.Entity<Department>().HasIndex(d => d.Code).IsUnique();
            builder.Entity<Department>().HasIndex(d => d.Slug).IsUnique();

            builder.Entity<Major>().HasIndex(m => m.Code).IsUnique();
            builder.Entity<Major>().HasIndex(m => m.Slug).IsUnique();
            builder.Entity<Major>()
                .HasOne(m => m.Department)
                .WithMany(d => d.Majors)
                .HasForeignKey(m => m.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subject>()
                .HasOne(s => s.Major)
                .WithMany(m => m.Subjects)
                .HasForeignKey(s => s.MajorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Lecturer>()
                .HasOne(l => l.Department)
                .WithMany(d => d.Lecturers)
                .HasForeignKey(l => l.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<NewsCategory>().HasIndex(c => c.Slug).IsUnique();
            builder.Entity<SiteSetting>().HasIndex(s => s.Key).IsUnique();
            builder.Entity<News>().HasIndex(n => n.Slug).IsUnique();
            builder.Entity<News>()
                .HasOne(n => n.Category)
                .WithMany(c => c.NewsItems)
                .HasForeignKey(n => n.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<News>()
                .HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Announcement>()
                .HasOne(a => a.Category)
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>().HasIndex(e => e.Slug).IsUnique();

            // ---- Numeric precision ----
            // No explicit column type is declared for the score/tuition columns.
            //
            // They used to be decimal with HasColumnType("decimal(4,2)"). That hardcoded a
            // SQL Server type name, and on SQLite the provider maps decimal to TEXT and
            // refuses to ORDER BY it at all. The properties are now double, which needs no
            // per-provider mapping and sorts correctly on both providers.
        }
    }
}
