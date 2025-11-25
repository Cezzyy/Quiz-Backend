namespace OnlineQuiz.DTOs
{
    public class ActivityLogStatisticsDto
    {
        public long TotalActions { get; set; }
        public long UniqueUsers { get; set; }
        public Dictionary<string, int> ActionsByType { get; set; } = new();
        public Dictionary<string, int> EntitiesAffected { get; set; } = new();
        public List<ActivityLogDto> RecentActivity { get; set; } = new();
    }
}
