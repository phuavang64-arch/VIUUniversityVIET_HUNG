using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VIUUniversity.Data
{
    /// <summary>
    /// Used by the EF Core CLI (dotnet ef ...) to build a context without starting the
    /// web host. Without it, tooling would run Program.cs - including the startup seeding
    /// - against whatever connection string is in appsettings.json.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var connString = "Data Source=VIUUniversity.db";
            optionsBuilder.UseSqlite(connString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}