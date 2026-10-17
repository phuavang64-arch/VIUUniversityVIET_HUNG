using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VIUUniversity.Services.Interfaces;

namespace VIUUniversity.Controllers
{
    /// <summary>
    /// Serves robots.txt and sitemap.xml, built from the database on every request.
    /// </summary>
    [Route("")]
    [AllowAnonymous]
    public class SitemapController : Controller
    {
        private readonly ISitemapService _sitemapService;

        public SitemapController(ISitemapService sitemapService) => _sitemapService = sitemapService;

        // GET: /robots.txt
        //
        // Served from code rather than dropped in wwwroot on purpose: the Sitemap: line
        // has to name the host the site is actually being served from. A committed
        // wwwroot/robots.txt would have localhost baked into it and quietly tell every
        // crawler that the sitemap lives on the developer's machine.
        [HttpGet("robots.txt")]
        public IActionResult Robots()
        {
            var origin = $"{Request.Scheme}://{Request.Host}";
            var body = string.Join("\n",
                "# robots.txt — Trường Đại học Công nghiệp Việt - Hung",
                "",
                "User-agent: *",
                "Allow: /",
                "",
                "# Khu vực quản trị, tài khoản và các API nội bộ: không có giá trị để",
                "# lập chỉ mục và không nên xuất hiện trong kết quả tìm kiếm.",
                "Disallow: /admin",
                "Disallow: /tai-khoan",
                "Disallow: /api/",
                "",
                "# Sitemap được sinh động từ database, không phải danh sách viết tay.",
                $"Sitemap: {origin}/sitemap.xml",
                "");

            return Content(body, "text/plain", Encoding.UTF8);
        }

        // GET: /sitemap.xml
        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var entries = await _sitemapService.BuildEntriesAsync();

            var origin = $"{Request.Scheme}://{Request.Host}";

            var settings = new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = new UTF8Encoding(false)
            };

            // Built in a MemoryStream rather than a StringBuilder on purpose: the
            // StringBuilder overload writes `encoding="utf-16"` into the XML declaration
            // (it reflects the builder, not the settings), while the bytes on the wire are
            // UTF-8. A crawler reading that declaration would decode the file wrongly.
            using var buffer = new MemoryStream();

            using (var writer = XmlWriter.Create(buffer, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                foreach (var entry in entries)
                {
                    writer.WriteStartElement("url");

                    writer.WriteStartElement("loc");
                    // Concatenated as-is: ISitemapService has already escaped each slug
                    // segment, and escaping the finished URL again would turn "%C4" into
                    // "%25C4". The XmlWriter escapes the text, which is what XML needs.
                    writer.WriteString(origin + entry.Location);
                    writer.WriteEndElement();

                    if (entry.LastModified is { } lastModified)
                    {
                        writer.WriteStartElement("lastmod");
                        // Dates are stored without a time zone, so they are published in
                        // the same wall-clock convention the rest of the site uses rather
                        // than pretending to an offset that was never recorded.
                        writer.WriteString(lastModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }

                    if (!string.IsNullOrWhiteSpace(entry.ChangeFrequency))
                    {
                        writer.WriteStartElement("changefreq");
                        writer.WriteString(entry.ChangeFrequency);
                        writer.WriteEndElement();
                    }

                    if (entry.Priority is { } priority)
                    {
                        writer.WriteStartElement("priority");
                        writer.WriteString(priority.ToString("0.0", CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            // Already-cached bytes: no charset parameter is appended, so the encoding
            // declared inside the XML is the one the crawler must use.
            return File(buffer.ToArray(), "application/xml");
        }
    }
}