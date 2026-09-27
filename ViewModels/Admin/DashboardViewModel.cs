namespace VIUUniversity.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public int TotalDepartments { get; set; }
        public int TotalMajors { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalNews { get; set; }
        public int TotalAnnouncements { get; set; }
        public int TotalEvents { get; set; }

        public List<MonthlyCount> NewsByMonth { get; set; } = new();
    }

    public class MonthlyCount
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
