using Microsoft.EntityFrameworkCore;
using VIUUniversity.Models;
using VIUUniversity.Services;

namespace VIUUniversity.Tests;

/// <summary>
/// News.Slug has a UNIQUE index but slugs are generated from the title, so two
/// articles with the same title collide. The service must resolve that instead of
/// letting the unique-constraint violation surface as a 500.
/// </summary>
public class NewsServiceTests : DatabaseTestBase
{
    private NewsService CreateService() => new(Db, new ContentSanitizer());

    private News NewNews(string title, string? slug = null, bool published = true) => new()
    {
        Title = title,
        Slug = slug ?? SlugHelper.GenerateSlug(title),
        Content = "<p>Nội dung</p>",
        Summary = "Tóm tắt",
        Category = NewCategory(),
        IsPublished = published
    };

    [Fact]
    public async Task EnsureUniqueSlug_ReturnsSameSlugWhenFree()
    {
        var service = CreateService();
        var other = NewNews("Một bài khác");
        Db.News.Add(other);
        await Db.SaveChangesAsync();

        // Nothing uses this slug yet, so it is returned unchanged.
        var result = await service.EnsureUniqueSlugAsync("bai-viet-dau-tien");

        Assert.Equal("bai-viet-dau-tien", result);
    }

    [Fact]
    public async Task EnsureUniqueSlug_ReturnsSameSlugForTheRecordBeingEdited()
    {
        var service = CreateService();
        var news = NewNews("Bài viết đầu tiên");
        Db.News.Add(news);
        await Db.SaveChangesAsync();

        // Editing an article keeps its own slug instead of being forced to -2.
        var result = await service.EnsureUniqueSlugAsync("bai-viet-dau-tien", news.Id);

        Assert.Equal("bai-viet-dau-tien", result);
    }

    [Fact]
    public async Task EnsureUniqueSlug_AppendsSuffixWhenSlugTaken()
    {
        var service = CreateService();
        Db.News.Add(NewNews("Bài viết đầu tiên"));
        await Db.SaveChangesAsync();

        var result = await service.EnsureUniqueSlugAsync("bai-viet-dau-tien");

        Assert.Equal("bai-viet-dau-tien-2", result);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTitle_DoesNotThrow()
    {
        var service = CreateService();

        var first = await service.CreateAsync(NewNews("Cùng một tiêu đề"));
        var second = await service.CreateAsync(NewNews("Cùng một tiêu đề"));

        Assert.Equal("cung-mot-tieu-de", first.Slug);
        Assert.Equal("cung-mot-tieu-de-2", second.Slug);
        Assert.Equal(2, await Db.News.CountAsync());
    }

    [Fact]
    public async Task SlugExists_IgnoresTheRecordBeingEdited()
    {
        var service = CreateService();
        var news = NewNews("Bài viết");
        Db.News.Add(news);
        await Db.SaveChangesAsync();

        Assert.True(await service.SlugExistsAsync(news.Slug));
        Assert.False(await service.SlugExistsAsync(news.Slug, news.Id));
    }

    [Fact]
    public async Task CreateAsync_SanitizesContentBeforeStoring()
    {
        var service = CreateService();
        var news = NewNews("Tiêu đề sạch");
        news.Content = "<p>Chào bạn</p><script>alert('xss')</script>";

        await service.CreateAsync(news);

        var stored = await Db.News.FirstAsync(n => n.Id == news.Id);
        Assert.Contains("Chào bạn", stored.Content);
        Assert.DoesNotContain("<script", stored.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotOverwriteViewCount()
    {
        var service = CreateService();
        var news = NewNews("Bài viết có lượt xem");
        await service.CreateAsync(news);

        // Mirror the real admin flow: the controller reads with GetByIdAsync (AsNoTracking),
        // so the instance passed to UpdateAsync is detached from the change tracker.
        Db.ChangeTracker.Clear();
        var detached = await service.GetByIdAsync(news.Id);
        Assert.NotNull(detached);

        // A visitor opens the article, incrementing ViewCount behind our back.
        await Db.News.Where(n => n.Id == news.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ViewCount, n => n.ViewCount + 1));

        detached!.Title = "Tiêu đề đã sửa";
        await service.UpdateAsync(detached);

        var stored = await Db.News.AsNoTracking().FirstAsync(n => n.Id == news.Id);
        Assert.Equal("Tiêu đề đã sửa", stored.Title);
        Assert.Equal(1, stored.ViewCount);
    }

    [Fact]
    public async Task GetBySlugAsync_IncrementsViewCount()
    {
        var service = CreateService();
        var news = NewNews("Bài viết phổ biến");
        await service.CreateAsync(news);

        await service.GetBySlugAsync(news.Slug);
        await service.GetBySlugAsync(news.Slug);

        Assert.Equal(2, await Db.News.Where(n => n.Id == news.Id).Select(n => n.ViewCount).FirstAsync());
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNullForUnpublishedNews()
    {
        var service = CreateService();
        var news = NewNews("Bản nháp", published: false);
        await service.CreateAsync(news);

        Assert.Null(await service.GetBySlugAsync(news.Slug));
    }

    [Fact]
    public async Task DeleteAsync_IsSoftDelete()
    {
        var service = CreateService();
        var news = NewNews("Sẽ bị xoá mềm");
        await service.CreateAsync(news);

        await service.DeleteAsync(news.Id);

        var stored = await Db.News.FirstAsync(n => n.Id == news.Id);
        Assert.True(stored.IsDeleted);
        Assert.Null(await service.GetBySlugAsync(news.Slug));
    }

    [Fact]
    public async Task GetPublishedAsync_IsCaseInsensitive()
    {
        var service = CreateService();
        await service.CreateAsync(NewNews("Hội thảo Quốc tế"));

        var upper = await service.GetPublishedAsync(1, 10, "QUỐC TẾ");
        var lower = await service.GetPublishedAsync(1, 10, "quốc tế");

        Assert.Equal(1, upper.TotalCount);
        Assert.Equal(1, lower.TotalCount);
    }
}