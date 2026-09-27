using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VIUUniversity.Services
{
    /// <summary>Turns "Công nghệ thông tin" into "cong-nghe-thong-tin" for clean SEO URLs.</summary>
    public static class SlugHelper
    {
        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var withoutDiacritics = sb.ToString()
                .Replace('đ', 'd').Replace('Đ', 'D');

            var slug = Regex.Replace(withoutDiacritics, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            slug = Regex.Replace(slug, @"-+", "-");

            return slug;
        }
    }
}
