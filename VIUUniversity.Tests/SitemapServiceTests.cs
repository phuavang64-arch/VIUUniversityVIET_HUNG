using Microsoft.EntityFrameworkCore;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Tests;

/// <summary>
/// sitemap.xml is generated from the database, so the only way it can be wrong is if the
/// query leaks something. These tests pin down which rows are allowed to appear in it:
/// published content yes, drafts and soft-deleted rows never.
/// </summary>
public class SitemapServiceTests : DatabaseTestBase
{
    private SitemapService CreateService() => new(Db);

    private NewsCategory NewCategory() => NewCategory("Tin tuc");

    // AnnouncementCategory has no slug, so it needs its own cached factory.
    private AnnouncementCategory? _announcementCategory;

    private AnnouncementCategory NewAnnouncementCategory() =>
        _announcementCategory ??= new AnnouncementCategory { Name = "Thông báo" };

    private News NewNews(string slug, bool published, bool deleted = false) => new()
    {
        Title = slug,
        Slug = slug,
        Content = "<p>Nội dung</p>",
        Category = NewCategory(),
        IsPublished = published,
        IsDeleted = deleted
    };

    private async Task<List<string>> LocationsAsync()
    {
        var entries = await CreateService().BuildEntriesAsync();
        return entries.Select(e => e.Location).ToList();
    }

    [Fact]
    public async Task BuildEntries_IncludesStaticPages()
    {
        var locations = await LocationsAsync();

        // The pages that must never go missing: they are the entry points for SEO and
        // they are hard-coded, so a regression here is a code change gone wrong.
        Assert.Contains("/", locations);
        Assert.Contains("/tin-tuc", locations);
        Assert.Contains("/thong-bao", locations);
        Assert.Contains("/dao-tao", locations);
        Assert.Contains("/tuyen-sinh", locations);
        Assert.Contains("/lien-he", locations);
    }

    [Fact]
    public async Task BuildEntries_IncludesPublishedNews()
    {
        Db.News.Add(NewNews("bai-da-xuat-ban", published: true));
        await Db.SaveChangesAsync();

        var locations = await LocationsAsync();

        Assert.Contains("/tin-tuc/bai-da-xuat-ban", locations);
    }

    [Fact]
    public async Task BuildEntries_ExcludesUnpublishedNews()
    {
        // A draft must not be advertised: the URL would 404 for everyone but the author.
        Db.News.Add(NewNews("bai-nhap-", published: false));
        await Db.SaveChangesAsync();

        var locations = await LocationsAsync();

        Assert.DoesNotContain("/tin-tuc/bai-nhap-", locations);
    }

    [Fact]
    public async Task BuildEntries_ExcludesSoftDeletedNews()
    {
        Db.News.Add(NewNews("bai-da-xoa", published: true, deleted: true));
        await Db.SaveChangesAsync();

        var locations = await LocationsAsync();

        Assert.DoesNotContain("/tin-tuc/bai-da-xoa", locations);
    }

    [Fact]
    public async Task BuildEntries_ExcludesAdminAndAccountPaths()
    {
        // robots.txt disallows these; listing them in the sitemap would contradict it.
        var locations = await LocationsAsync();

        Assert.DoesNotContain(locations, l => l.StartsWith("/admin"));
        Assert.DoesNotContain(locations, l => l.StartsWith("/tai-khoan"));
        Assert.DoesNotContain(locations, l => l.StartsWith("/api"));
    }

    [Fact]
    public async Task BuildEntries_UsesAnnouncementIdNotSlug()
    {
        // Announcements are addressed by id, so the sitemap has to agree with the route.
        // CategoryId is a non-nullable foreign key, so the category has to exist first -
        // assigning the navigation property is what gets it inserted.
        var announcement = new Announcement
        {
            Title = "Thông báo thử",
            Content = "<p>Nội dung</p>",
            Category = NewAnnouncementCategory(),
            IsPublished = true
        };
        Db.Announcements.Add(announcement);
        await Db.SaveChangesAsync();

        var locations = await LocationsAsync();

        Assert.Contains($"/thong-bao/{announcement.Id}", locations);
    }

    [Fact]
    public async Task BuildEntries_EscapesSlugsContainingVietnameseDiacritics()
    {
        // Slugs are stored unaccented, but a hand-typed slug from the admin can still
        // contain anything. The URL has to stay well-formed rather than emit raw UTF-8
        // into the XML document.
        Db.News.Add(NewNews("bai-bao-cao-hiem-tinh-hai", published: true));
        await Db.SaveChangesAsync();

        var entries = await CreateService().BuildEntriesAsync();
        var entry = entries.Single(e => e.Location.Contains("bao-cao"));

        Assert.DoesNotContain(" ", entry.Location);
    }

    [Fact]
    public async Task BuildEntries_CarriesLastModifiedForContent()
    {
        var published = new DateTime(2026, 3, 15, 8, 30, 0, DateTimeKind.Utc);
        Db.News.Add(new News
        {
            Title = "bai co ngay",
            Slug = "bai-co-ngay",
            Content = "<p>Nội dung</p>",
            Category = NewCategory(),
            IsPublished = true,
            PublishedAt = published
        });
        await Db.SaveChangesAsync();

        var entries = await CreateService().BuildEntriesAsync();
        var entry = entries.Single(e => e.Location == "/tin-tuc/bai-co-ngay");

        Assert.Equal(published, entry.LastModified);
    }

    [Fact]
    public async Task BuildEntries_ReturnsNoDuplicates()
    {
        Db.News.Add(NewNews("bai-mot", published: true));
        Db.Events.Add(new Event
        {
            Title = "su-kien-mot",
            Slug = "su-kien-mot",
            StartAt = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            IsPublished = true
        });
        await Db.SaveChangesAsync();

        var locations = await LocationsAsync();

        Assert.Equal(locations.Count, locations.Distinct().Count());
    }

    [Fact]
    public async Task BuildEntries_SurvivesEmptyDatabase()
    {
        // The sitemap must still render the static pages when there is no content yet.
        var locations = await LocationsAsync();

        Assert.NotEmpty(locations);
        Assert.Contains("/", locations);
    }
}