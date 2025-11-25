namespace OnlineQuiz.DTOs
{
    public class CreateActivityLogDto
    {
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public long? EntityId { get; set; }
        public string? Description { get; set; }
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
