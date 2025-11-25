using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("ActivityLog")]
    public class ActivityLog : BaseModel
    {
        [PrimaryKey("ActivityLogId")]
        [Column("ActivityLogId")]
        public long ActivityLogId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Action")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("Entity")]
        public string Entity { get; set; } = string.Empty;

        [Column("EntityId")]
        public long? EntityId { get; set; }

        [MaxLength(500)]
        [Column("Description")]
        public string? Description { get; set; }

        [Column("OldValues")]
        public string? OldValues { get; set; }

        [Column("NewValues")]
        public string? NewValues { get; set; }

        [MaxLength(45)]
        [Column("IpAddress")]
        public string? IpAddress { get; set; }

        [MaxLength(255)]
        [Column("UserAgent")]
        public string? UserAgent { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
