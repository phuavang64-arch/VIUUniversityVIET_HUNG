using VIUUniversity.Html;

namespace VIUUniversity.Tests;

/// <summary>
/// Video nhúng phải chỉ nhận nguồn được phép. Nếu không, một URL bất kỳ sẽ trở
/// thành <c>&lt;iframe&gt;</c> do chính site phục vụ — tức là một đường dẫn tải
/// mã độc vào trang của trường.
/// </summary>
public class VideoExtensionsTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    public void Youtube_DuocChuyenThanhEmbed(string url)
    {
        var src = VideoExtensions.EmbedSrc(url);

        Assert.NotNull(src);
        Assert.StartsWith("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", src);
    }

    [Fact]
    public void Youtube_KhongGioiHanDomain()
    {
        // youtu.be.evil.com không phải youtu.be — regex phải khớp đúng domain.
        Assert.Null(VideoExtensions.EmbedSrc("https://youtu.be.evil.com/watch?v=dQw4w9WgXcQ"));
    }

    [Theory]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://www.vimeo.com/123456789")]
    public void Vimeo_DuocChuyenThanhEmbed(string url)
    {
        var src = VideoExtensions.EmbedSrc(url);

        Assert.NotNull(src);
        Assert.Equal("https://player.vimeo.com/video/123456789", src);
    }

    [Fact]
    public void Mp4_Https_DuocChapNhan()
    {
        Assert.Equal("https://media.viu.edu.vn/video.mp4",
            VideoExtensions.EmbedSrc("https://media.viu.edu.vn/video.mp4"));
    }

    [Fact]
    public void Mp4_DuongDanTuongDoi_BiBoQua()
    {
        // Đường dẫn tương đối có thể trỏ tới bất kỳ tệp nào trong wwwroot.
        Assert.Null(VideoExtensions.EmbedSrc("/uploads/video.mp4"));
    }

    [Theory]
    [InlineData("https://evil.example.com/steal")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://youtube.com.evil.com/watch?v=x")]
    [InlineData("")]
    [InlineData(null)]
    public void URLKhongHopLe_BiBoQua(string? url)
    {
        Assert.Null(VideoExtensions.EmbedSrc(url));
    }

    [Fact]
    public void IsVideo_KhongNhamURLChuoi()
    {
        Assert.True(VideoExtensions.IsVideo("https://youtu.be/dQw4w9WgXcQ"));
        Assert.False(VideoExtensions.IsVideo("https://example.com"));
        Assert.False(VideoExtensions.IsVideo(null));
    }
}