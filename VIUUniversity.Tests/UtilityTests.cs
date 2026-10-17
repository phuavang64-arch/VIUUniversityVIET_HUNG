using Microsoft.AspNetCore.Http;
using VIUUniversity.Services;
using VIUUniversity.ViewModels.Admin;

namespace VIUUniversity.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Ngôn ngữ Anh", "ngon-ngu-anh")]
    [InlineData("Kỹ thuật cơ khí", "ky-thuat-co-khi")]
    [InlineData("Tin tức & Sự kiện", "tin-tuc-su-kien")]
    [InlineData("  Nhiều   khoảng trắng  ", "nhieu-khoang-trang")]
    [InlineData("ABC 123", "abc-123")]
    public void GenerateSlug_ProducesUrlSafeOutput(string input, string expected)
    {
        Assert.Equal(expected, SlugHelper.GenerateSlug(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateSlug_ReturnsEmptyForBlank(string? input)
    {
        Assert.Equal(string.Empty, SlugHelper.GenerateSlug(input!));
    }

    [Fact]
    public void GenerateSlug_IsIdempotent()
    {
        var once = SlugHelper.GenerateSlug("Chương trình đào tạo Ngành Kế toán");
        var twice = SlugHelper.GenerateSlug(once);

        Assert.Equal(once, twice);
    }
}

/// <summary>
/// Dashboard charts order by (Year, Month). Ordering by the "M/YYYY" label string
/// sorts lexicographically, which puts 10/2026 before 2/2026.
/// </summary>
public class MonthlyCountTests
{
    [Fact]
    public void SortsChronologicallyUsingYearAndMonth()
    {
        var data = new List<MonthlyCount>
        {
            new() { Label = "10/2026", Year = 2026, Month = 10, Count = 1 },
            new() { Label = "2/2026",  Year = 2026, Month = 2,  Count = 1 },
            new() { Label = "1/2026",  Year = 2026, Month = 1,  Count = 1 },
            new() { Label = "12/2025", Year = 2025, Month = 12, Count = 1 },
        };

        var sorted = data
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => x.Label)
            .ToList();

        Assert.Equal(new[] { "12/2025", "1/2026", "2/2026", "10/2026" }, sorted);
    }

    [Fact]
    public void LexicographicSortWouldBeWrong()
    {
        // Documents the bug this guards against.
        var labels = new[] { "10/2026", "2/2026" };

        Assert.Equal("10/2026", labels.OrderBy(l => l).First());
    }
}

public class ImageUploadHelperTests
{
    private static FormFile MakeFile(byte[] content, string fileName)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "f", fileName);
    }

    [Fact]
    public async Task RejectsOversizedFile()
    {
        // FormFile.Length is derived from the stream, so a genuinely oversized stream is
        // needed. ImageUploadHelper.MaxImageBytes is 5 MB, which is cheap to allocate here.
        var oversized = new byte[ImageUploadHelper.MaxImageBytes + 1];
        var file = MakeFile(oversized, "big.jpg");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ImageUploadHelper.SaveAsync(file, Path.GetTempPath(), "test"));
    }

    [Fact]
    public async Task RejectsDisallowedExtension()
    {
        var file = MakeFile(new byte[10], "malicious.svg");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ImageUploadHelper.SaveAsync(file, Path.GetTempPath(), "test"));
    }

    [Fact]
    public async Task RejectsHtmlDisguisedAsJpeg()
    {
        // The whole point of magic-byte checking: a ".jpg" that is really HTML.
        var html = System.Text.Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>");
        var file = MakeFile(html, "sneaky.jpg");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ImageUploadHelper.SaveAsync(file, Path.GetTempPath(), "test"));
    }

    [Fact]
    public async Task RejectsEmptyFile()
    {
        var file = MakeFile(Array.Empty<byte>(), "empty.jpg");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ImageUploadHelper.SaveAsync(file, Path.GetTempPath(), "test"));
    }
}