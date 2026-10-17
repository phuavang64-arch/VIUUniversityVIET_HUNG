namespace VIUUniversity.Services.Interfaces
{
    /// <summary>
    /// A single entry of sitemap.xml.
    /// </summary>
    /// <param name="Location">Path on this site, always starting with '/'.</param>
    /// <param name="LastModified">Last change date, or null when the date is not known.</param>
    /// <param name="ChangeFrequency">Crawler hint. Null leaves the tag out.</param>
    /// <param name="Priority">Crawler hint in the 0.0-1.0 range. Null leaves the tag out.</param>
    public readonly record struct SitemapEntry(
        string Location,
        DateTime? LastModified,
        string? ChangeFrequency = null,
        double? Priority = null);

    public interface ISitemapService
    {
        /// <summary>
        /// Builds the list of publicly indexable URLs: the static pages plus every
        /// published news article, announcement, event, department and major.
        /// </summary>
        Task<IReadOnlyList<SitemapEntry>> BuildEntriesAsync();
    }
}