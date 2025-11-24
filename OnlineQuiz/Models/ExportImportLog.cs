using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("ExportImportLog")]
    public class ExportImportLog
    {
        [Key]
        [Column("LogId")]
        public int LogId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Type")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("FileName")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Pending";

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("CompletedAt")]
        public DateTime? CompletedAt { get; set; }

        [Column("ErrorMessage")]
        public string? ErrorMessage { get; set; }
    }
}
