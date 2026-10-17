using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VIUUniversity.Html;

/// <summary>
/// Nhúng video từ URL do quản trị viên dán vào ô VideoUrl.
///
/// Chỉ chấp nhận ba nguồn: YouTube, Vimeo và tệp .mp4 trực tiếp. Mọi URL khác bị bỏ
/// qua — nếu không, một URL bất kỳ sẽ trở thành <c>&lt;iframe&gt;</c> do chính site
/// phục vụ, tức là một đường dẫn tải mã độc.
///
/// URL được đưa vào thuộc tính <c>src</c> của iframe nên phải được HTML-encode; phần
/// còn lại (khung, thuộc tính) do mã nguồn tự viết nên không cần tin chắc đầu vào.
/// </summary>
public static class VideoExtensions
{
    private static readonly Regex Youtube = new(
        @"^(?:https?://)?(?:www\.)?(?:youtube\.com/(?:watch\?.*v=|embed/|shorts/)|youtu\.be/)([\w-]{6,})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Vimeo = new(
        @"^(?:https?://)?(?:www\.)?vimeo\.com/(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Trả về URL nhúng đã encode sẵn, hoặc null nếu URL không thuộc nguồn được phép.
    /// </summary>
    public static string? EmbedSrc(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        url = url.Trim();

        var yt = Youtube.Match(url);
        if (yt.Success)
        {
            // rel=0: không tự gợi ý video khác của kênh sau khi phát xong.
            // modestbranding=1: bớt logo YouTube đè lên thanh điều khiển.
            return "https://www.youtube-nocookie.com/embed/" + yt.Groups[1].Value
                 + "?rel=0&modestbranding=1";
        }

        var vm = Vimeo.Match(url);
        if (vm.Success)
        {
            return "https://player.vimeo.com/video/" + vm.Groups[1].Value;
        }

        // MP4 trực tiếp: chỉ nhận https, không nhận đường dẫn tương đối (có thể trỏ
        // tới bất kỳ tệp nào trong wwwroot).
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return null;
    }

    /// <summary>
    /// Khung video 16:9. Dùng padding-top thay vì aspect-ratio vì một số trình duyệt
    /// cũ chưa hỗ trợ; iframe đặt tuyệt đối để lấp đầy khung.
    /// </summary>
    public static IHtmlContent Embed(this IHtmlHelper html, string? url, string? title = null)
    {
        var src = EmbedSrc(url);
        if (src is null)
        {
            return HtmlString.Empty;
        }

        var label = string.IsNullOrWhiteSpace(title) ? "Video" : title;
        var encoded = System.Net.WebUtility.HtmlEncode(src);

        return new HtmlString(
            "<div class=\"viu-video\">"
            + "<iframe src=\"" + encoded + "\""
            + " title=\"" + System.Net.WebUtility.HtmlEncode(label) + "\""
            + " loading=\"lazy\""
            + " allow=\"accelerometer; autoplay; clipboard-write; encrypted-media;"
            + " gyroscope; picture-in-picture\""
            + " allowfullscreen"
            + " referrerpolicy=\"strict-origin-when-cross-origin\">"
            + "</iframe></div>");
    }

    /// <summary>
    /// True khi URL là video hợp lệ — để view quyết định có hiện nhãn "Video" không.
    /// </summary>
    public static bool IsVideo(string? url) => EmbedSrc(url) is not null;
}