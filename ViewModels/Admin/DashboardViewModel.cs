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
        public int TotalAdmissionLeads { get; set; }
        public int TotalPendingLeads { get; set; }
        public int TotalGalleryImages { get; set; }
        public int TotalContactMessages { get; set; }
        public int TotalUnreadMessages { get; set; }

        public List<MonthlyCount> NewsByMonth { get; set; } = new();
        public List<MonthlyCount> LeadsByMonth { get; set; } = new();
    }

    public class MonthlyCount
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }

        /// <summary>
        /// Sort key. Label is a "M/YYYY" string, so ordering by it would sort
        /// lexicographically ("10/2026" before "2/2026"). Charts must order on this.
        /// </summary>
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
