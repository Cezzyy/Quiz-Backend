namespace OnlineQuiz.DTOs
{
    public class ActivityLogFilterDto
    {
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public string? Entity { get; set; }
        public long? EntityId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
