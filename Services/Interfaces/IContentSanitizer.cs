namespace VIUUniversity.Services.Interfaces
{
    /// <summary>
    /// Central place for turning untrusted input into safe output.
    ///
    /// The Admin CMS stores rich text as raw HTML (News.Content, Announcement.Content,
    /// Event.Description) and those values are later emitted with @Html.Raw. Without a
    /// sanitizer anywhere in the write path, a single compromised Editor account is
    /// enough to run script in every visitor's browser. Sanitizing on write means the
    /// stored value is already safe, so @Html.Raw becomes acceptable in the views.
    /// </summary>
    public interface IContentSanitizer
    {
        /// <summary>
        /// Strips scripts, event handlers, javascript: URLs and any other unsafe
        /// construct from rich-text HTML while keeping the formatting tags editors need.
        /// </summary>
        string SanitizeHtml(string? html);

        /// <summary>
        /// Escapes a plain-text value for safe interpolation into an HTML string.
        /// Use this for short text fields (titles, names, descriptions) that are
        /// assembled into markup on the server side.
        /// </summary>
        string Encode(string? text);

        /// <summary>
        /// Sanitizes a rich-text value that may also be used outside of HTML, e.g. as a
        /// meta description or a plain-text preview.
        /// </summary>
        string ToPlainText(string? html, int maxLength = 0);
    }
}