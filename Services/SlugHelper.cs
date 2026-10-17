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

            // 'đ'/'Đ' không phân rã được trong FormD (NFD) nên phải thay trước —
            // nếu không, bước bỏ dấu sẽ xoá mất chữ 'đ' và cho ra slug sai như
            // "ien-ien-tu" thay vì "dien-dien-tu".
            var lowered = input.ToLowerInvariant()
                               .Replace('đ', 'd').Replace('Đ', 'D');

            var normalized = lowered.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var withoutDiacritics = sb.ToString();

            var slug = Regex.Replace(withoutDiacritics, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            slug = Regex.Replace(slug, @"-+", "-");

            return slug;
        }
    }
}
