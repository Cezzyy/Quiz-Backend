using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class CreateNotificationDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // Quiz, Course, System, Reminder

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateNotificationDto
    {
        public bool IsRead { get; set; }
    }

    public class NotificationResponseDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
