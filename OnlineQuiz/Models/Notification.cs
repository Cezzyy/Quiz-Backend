using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Notification")]
    public class Notification
    {
        [Key]
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
