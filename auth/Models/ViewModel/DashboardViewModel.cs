namespace Auth.Models.ViewModel
{
    public class DashboardViewModel
    {
        public string UserFullName { get; set; }
        public int PendingCount { get; set; }
        public int SkillsCount { get; set; }
        public int UpcomingTrainings { get; set; }
        public int UnreadNotifications { get; set; }

        public List<ActivityItem> RecentActivities { get; set; } = new();
        public List<NotificationItem> Notifications { get; set; } = new();
    }


    public class ActivityItem
    {
        public string Message { get; set; }
        public string Icon { get; set; }
        public string Link { get; set; }
        public string TimeAgo { get; set; }
        public bool IsRead { get; set; }
    }

    public class NotificationItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Icon { get; set; }
        public string Link { get; set; }
        public string TimeAgo { get; set; }
        public bool IsRead { get; set; }
    }
}
