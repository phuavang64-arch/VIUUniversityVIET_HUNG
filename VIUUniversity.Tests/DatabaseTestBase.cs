using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VIUUniversity.Data;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Tests;

/// <summary>
/// Base class providing a throwaway in-memory SQLite database per test.
/// Each test gets its own connection so they can run in parallel.
/// </summary>
public abstract class DatabaseTestBase : IDisposable
{
    private readonly SqliteConnection _connection;

    protected DatabaseTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();
    }

    protected ApplicationDbContext Db { get; }

    // One category per test. Creating a new one per news item would violate the UNIQUE
    // index on NewsCategories.Slug and mask what the test is actually asserting.
    private NewsCategory? _category;

    protected NewsCategory NewCategory(string name = "Tin tuc") =>
        _category ??= new NewsCategory { Name = name, Slug = "tin-tuc" };

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}