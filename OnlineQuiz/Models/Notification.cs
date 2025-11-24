using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Notification")]
    public class Notification : BaseModel
    {
        [PrimaryKey("NotificationId")]
        [Column("NotificationId")]
        public int NotificationId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Type")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("Message")]
        public string Message { get; set; } = string.Empty;

        [Column("Is_Read")]
        public bool IsRead { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
