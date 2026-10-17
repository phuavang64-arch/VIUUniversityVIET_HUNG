namespace VIUUniversity.ViewModels
{
    public class SearchResultItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Date { get; set; }
        public string BadgeClass { get; set; } = "bg-primary";
    }

    public class SearchPageViewModel
    {
        public string? Query { get; set; }
        public List<SearchResultItem> Results { get; set; } = new();
        public int TotalCount => Results.Count;
        public int NewsCount { get; set; }
        public int AnnouncementCount { get; set; }
        public int EventCount { get; set; }
        public int MajorCount { get; set; }
    }
}
