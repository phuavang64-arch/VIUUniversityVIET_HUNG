using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql;

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

            var connString = "Server=localhost;Port=3306;Database=VIUUniversityDB;Uid=root;Pwd=;";
            optionsBuilder.UseMySql(connString, new MySqlServerVersion(new Version(8, 0, 36)));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}