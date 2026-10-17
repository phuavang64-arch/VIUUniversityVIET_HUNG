using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;

namespace VIUUniversity.Data
{
    /// <summary>
    /// Brings the database schema up to date on startup, then runs the seeder.
    ///
    /// History: this used to call <c>EnsureCreatedAsync()</c> plus a hand-written
    /// <c>CREATE TABLE IF NOT EXISTS GalleryImages</c> statement. EnsureCreated only builds a
    /// schema for an EMPTY database, so once the file existed no model change could ever
    /// reach the database again - which is exactly why a raw SQL patch was needed to add a
    /// single table. The application now uses real EF Core migrations, and this class adopts
    /// databases that were created the old way, without touching their data.
    /// </summary>
    public static class DatabaseInitializer
    {
        private const string HistoryTable = "__EFMigrationsHistory";

        public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();

            await MigrateWithLegacyAdoptionAsync(db);

            await SeedData.InitializeAsync(services, config);
        }

        private static async Task MigrateWithLegacyAdoptionAsync(ApplicationDbContext db)
        {
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
                return;

            // A database built by the old EnsureCreated() path has tables but no
            // __EFMigrationsHistory at all. Recording the baseline migration as applied
            // makes the existing schema the starting point, so only genuinely new
            // migrations run from here on and no rows are destroyed.
            if (!await TableExistsAsync(db, HistoryTable)
                && await TableExistsAsync(db, "News"))
            {
                await StampBaselineAsync(db);
            }

            await db.Database.MigrateAsync();
        }

        /// <summary>
        /// Catalog lookup rather than a LINQ query: probing for a table by running a query
        /// against it logs a "fail:" entry every startup for a perfectly normal new database.
        /// </summary>
        private static async Task<bool> TableExistsAsync(ApplicationDbContext db, string tableName)
        {
            var connection = db.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;

            if (!wasOpen)
                await connection.OpenAsync();

            try
            {
                var command = connection.CreateCommand();
                command.CommandText = db.Database.IsSqlite()
                    ? "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@t;"
                    : "SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t;";

                var param = command.CreateParameter();
                param.ParameterName = "@t";
                param.Value = tableName;
                command.Parameters.Add(param);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt64(result) > 0;
            }
            finally
            {
                if (!wasOpen)
                    await connection.CloseAsync();
            }
        }

        /// <summary>
        /// Writes the baseline migration id into __EFMigrationsHistory, creating the table
        /// if the old EnsureCreated path never made one.
        /// </summary>
        private static async Task StampBaselineAsync(ApplicationDbContext db)
        {
            var baseline = db.Database.GetMigrations().FirstOrDefault();
            if (baseline == null)
                return;

            var isSqlite = db.Database.IsSqlite();
            var providerVersion = System.Environment.Version.ToString();

            var createHistory = isSqlite
                ? @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                        ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                        ""ProductVersion"" TEXT NOT NULL);"
                : @"IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
                    CREATE TABLE [dbo].[__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY,
                        [ProductVersion] nvarchar(32) NOT NULL);";

            var insertHistory = isSqlite
                ? @"INSERT OR REPLACE INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ({0}, {1});"
                : @"INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1});";

            await db.Database.ExecuteSqlRawAsync(createHistory);
            await db.Database.ExecuteSqlRawAsync(insertHistory, baseline, providerVersion);
        }
    }
}