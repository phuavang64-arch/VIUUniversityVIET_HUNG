using VIUUniversity.Services;

namespace VIUUniversity.Tests;

/// <summary>
/// The sanitizer is the single control that makes @Html.Raw safe in the news,
/// announcement and event detail views. If any of these regress, a compromised
/// Editor account becomes stored XSS against every visitor.
/// </summary>
public class ContentSanitizerTests
{
    private readonly ContentSanitizer _sanitizer = new();

    [Theory]
    [InlineData("<script>alert('xss')</script>", "alert")]
    [InlineData("<SCRIPT>alert(1)</SCRIPT>", "alert")]
    [InlineData("<img src=x onerror=alert(1)>", "onerror")]
    [InlineData("<a href=\"javascript:alert(1)\">click</a>", "javascript:")]
    [InlineData("<div onclick=\"steal()\">x</div>", "onclick")]
    [InlineData("<iframe src=\"https://evil.example/x\"></iframe>", "evil.example")]
    [InlineData("<body onload=alert(1)>", "onload")]
    public void RemovesActiveContent(string input, string mustNotContain)
    {
        var result = _sanitizer.SanitizeHtml(input);

        Assert.DoesNotContain(mustNotContain, result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<p>Xin chào các bạn</p>", "p")]
    [InlineData("<strong>Đập mạnh</strong> và <em>nghiêng</em>", "strong")]
    [InlineData("<ul><li>Mục 1</li></ul>", "li")]
    [InlineData("<a href=\"https://viu.edu.vn\">Trường</a>", "href")]
    public void KeepsLegitimateFormatting(string input, string mustContain)
    {
        var result = _sanitizer.SanitizeHtml(input);

        Assert.Contains(mustContain, result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HandlesEmptyInput(string? input)
    {
        Assert.Equal(string.Empty, _sanitizer.SanitizeHtml(input));
    }

    [Fact]
    public void Encode_EscapesHtmlSpecialCharacters()
    {
        var result = _sanitizer.Encode("<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void Encode_HandlesNullAndEmpty()
    {
        Assert.Equal(string.Empty, _sanitizer.Encode(null));
        Assert.Equal(string.Empty, _sanitizer.Encode(string.Empty));
    }

    [Theory]
    [InlineData("<p>Đại học Công nghiệp</p>", "Đại học Công nghiệp")]
    [InlineData("Dòng 1<br/>Dòng 2", "Dòng 1 Dòng 2")]
    public void ToPlainText_StripsMarkup(string input, string expected)
    {
        Assert.Equal(expected, _sanitizer.ToPlainText(input));
    }

    [Fact]
    public void ToPlainText_TruncatesAndAddsEllipsis()
    {
        var result = _sanitizer.ToPlainText("<p>Một hai ba bốn năm sáu bảy tám chín mười</p>", 20);

        Assert.EndsWith("…", result);
        Assert.True(result.Length <= 21);
    }

    [Fact]
    public void SanitizeHtml_RemovesEventHandlersFromAllowedTags()
    {
        // <p> is allowed, but the handler on it must not survive.
        var result = _sanitizer.SanitizeHtml("<p onmouseover=\"hack()\">Nội dung</p>");

        Assert.DoesNotContain("onmouseover", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hack()", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeHtml_KeepsTrustedEmbedHostOnly()
    {
        var trusted = _sanitizer.SanitizeHtml("<iframe src=\"https://www.youtube.com/embed/abc\"></iframe>");
        Assert.Contains("youtube.com", trusted);

        var untrusted = _sanitizer.SanitizeHtml("<iframe src=\"https://attacker.example/embed\"></iframe>");
        Assert.DoesNotContain("attacker.example", untrusted);
    }
}