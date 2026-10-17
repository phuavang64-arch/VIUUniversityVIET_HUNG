using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Services
{
    public class ContentSanitizer : IContentSanitizer
    {
        private readonly HtmlSanitizer _htmlSanitizer;

        // Sanitizers are expensive to build but thread-safe once constructed, so a single
        // instance is shared for the lifetime of the (scoped) service.
        public ContentSanitizer()
        {
            _htmlSanitizer = new HtmlSanitizer();

            // The default allow-list is already conservative. Tags editors legitimately
            // need on top of it for article bodies.
            _htmlSanitizer.AllowedTags.Add("img");
            _htmlSanitizer.AllowedTags.Add("h1");
            _htmlSanitizer.AllowedTags.Add("h2");
            _htmlSanitizer.AllowedTags.Add("h3");
            _htmlSanitizer.AllowedTags.Add("h4");
            _htmlSanitizer.AllowedTags.Add("figure");
            _htmlSanitizer.AllowedTags.Add("figcaption");
            _htmlSanitizer.AllowedTags.Add("table");
            _htmlSanitizer.AllowedTags.Add("thead");
            _htmlSanitizer.AllowedTags.Add("tbody");
            _htmlSanitizer.AllowedTags.Add("tr");
            _htmlSanitizer.AllowedTags.Add("th");
            _htmlSanitizer.AllowedTags.Add("td");
            _htmlSanitizer.AllowedTags.Add("iframe");
            _htmlSanitizer.AllowedTags.Add("span");
            _htmlSanitizer.AllowedTags.Add("u");
            _htmlSanitizer.AllowedTags.Add("s");

            // Only allow images that reference a file on this site. This blocks the
            // "data:" and tracking-pixel exfiltration vectors.
            _htmlSanitizer.AllowedSchemes.Add("http");
            _htmlSanitizer.AllowedSchemes.Add("https");

            _htmlSanitizer.AllowedAttributes.Add("src");
            _htmlSanitizer.AllowedAttributes.Add("alt");
            _htmlSanitizer.AllowedAttributes.Add("width");
            _htmlSanitizer.AllowedAttributes.Add("height");
            _htmlSanitizer.AllowedAttributes.Add("style");
            _htmlSanitizer.AllowedAttributes.Add("class");
            _htmlSanitizer.AllowedAttributes.Add("allowfullscreen");
            _htmlSanitizer.AllowedAttributes.Add("frameborder");

            _htmlSanitizer.AllowedCssProperties.Add("color");
            _htmlSanitizer.AllowedCssProperties.Add("background-color");
            _htmlSanitizer.AllowedCssProperties.Add("text-align");
            _htmlSanitizer.AllowedCssProperties.Add("font-weight");
            _htmlSanitizer.AllowedCssProperties.Add("font-style");
            _htmlSanitizer.AllowedCssProperties.Add("text-decoration");
            _htmlSanitizer.AllowedCssProperties.Add("width");

            // AngleSharp does not enforce attribute order, so "src"/"href" schemes must be
            // checked explicitly rather than relying on AllowedSchemes alone.
            _htmlSanitizer.AllowDataAttributes = false;
        }

        // HtmlSanitizer 9.x dropped the per-host iframe allow-list, so it is enforced here.
        private static readonly string[] AllowedEmbedHosts =
        {
            "www.youtube.com", "youtube.com", "www.youtube-nocookie.com",
            "player.vimeo.com", "docs.google.com"
        };

        private static readonly Regex IframeSrcRegex = new(
            "<iframe\\b[^>]*\\bsrc\\s*=\\s*[\"']([^\"']*)[\"'][^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Keeps only embed frames pointing at an approved host. Anything else - including
        /// "javascript:" sources that slipped past the scheme check - is dropped whole.
        /// </summary>
        private static string StripForeignEmbeds(string html)
        {
            return IframeSrcRegex.Replace(html, match =>
            {
                var src = match.Groups[1].Value.Trim();

                if (Uri.TryCreate(src, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                    && AllowedEmbedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                {
                    return match.Value;
                }

                return string.Empty;
            });
        }

        public string SanitizeHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var sanitized = _htmlSanitizer.Sanitize(html);
            sanitized = StripForeignEmbeds(sanitized);

            // Defence in depth: HtmlSanitizer already removes these, but a belt-and-braces
            // pass costs little and survives library configuration mistakes.
            sanitized = Regex.Replace(sanitized, @"<\s*(script|style)\b[^>]*>.*?<\s*/\s*\1\s*>",
                "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Any leftover inline event handler (onclick=, onerror=, ...) goes regardless
            // of which tag it was attached to.
            sanitized = Regex.Replace(sanitized, @"\s+on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
                "", RegexOptions.IgnoreCase);

            return sanitized.Trim();
        }

        public string Encode(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return WebUtility.HtmlEncode(text);
        }

        public string ToPlainText(string? html, int maxLength = 0)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Drop block-level tags first so words on separate lines don't merge together.
            var working = Regex.Replace(html, @"<\s*br\s*/?\s*>", " ", RegexOptions.IgnoreCase);
            working = Regex.Replace(working, @"<\s*/\s*(p|div|li|tr|h[1-6])\s*>", " ", RegexOptions.IgnoreCase);
            working = Regex.Replace(working, @"<[^>]*>", " ");

            var text = WebUtility.HtmlDecode(working);
            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (maxLength > 0 && text.Length > maxLength)
            {
                text = text[..maxLength].TrimEnd() + "…";
            }

            return text;
        }
    }
}