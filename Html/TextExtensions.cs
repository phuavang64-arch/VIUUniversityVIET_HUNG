using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VIUUniversity.Html;

/// <summary>
/// Helpers for plain-text columns written by admins or by the seed (major intro,
/// skills, career, duration). Those columns hold UTF-8 text with paragraphs
/// separated by a blank line, NOT markup.
/// </summary>
public static class TextExtensions
{
    /// <summary>
    /// Renders blank-line separated text as a sequence of <c>&lt;p&gt;</c>.
    /// Everything is HTML-encoded — the input is treated as text, never as markup.
    /// Returns an empty string for null/blank input so callers can use the result
    /// directly in a truthiness check.
    /// </summary>
    public static IHtmlContent Paragraphs(this IHtmlHelper html, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return HtmlString.Empty;
        }

        var builder = new StringBuilder(text.Length + 32);
        foreach (var block in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = block.Replace("\r", " ").Trim();
            if (trimmed.Length == 0)
                continue;

            builder.Append("<p>")
                   .Append(HtmlEncode(trimmed))
                   .Append("</p>");
        }

        return new HtmlString(builder.ToString());
    }

    /// <summary>
    /// Takes the first paragraph only — for short one-line fields such as the
    /// "career" blurb shown in a sidebar card.
    /// </summary>
    public static IHtmlContent FirstParagraph(this IHtmlHelper html, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return HtmlString.Empty;
        }

        var first = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        if (first.Length == 0)
        {
            return HtmlString.Empty;
        }

        return new HtmlString("<p>" + HtmlEncode(first) + "</p>");
    }

    /// <summary>
    /// True when the field has real content. Lets a view write
    /// <c>@if (Html.HasText(Model.SkillsDescription)) { ... }</c> instead of
    /// repeating the null check and accidentally rendering a heading over an
    /// empty box.
    /// </summary>
    public static bool HasText(this IHtmlHelper html, string? text) =>
        !string.IsNullOrWhiteSpace(text);

    /// <summary>
    /// Plain text of the first paragraph, cut at a word boundary and ending in an
    /// ellipsis. Returns an encoded string (not markup) so callers can drop it into
    /// <c>element.TextContent</c> in a view.
    /// </summary>
    /// <param name="maxChars">Approximate budget; the result can exceed it by one word.</param>
    public static string Excerpt(this IHtmlHelper html, string? text, int maxChars = 160)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var first = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)[0]
                        .Replace('\r', ' ')
                        .Trim();

        if (first.Length <= maxChars)
        {
            return first;
        }

        var cut = first[..maxChars];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxChars / 2)
        {
            cut = cut[..lastSpace];
        }

        return cut.TrimEnd(' ', ',', ';', ':', '.', '–', '—') + "…";
    }

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value)
            .Replace("\n", "<br />", StringComparison.Ordinal);
}