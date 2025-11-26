using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("ExportImportLog")]
    public class ExportImportLog : BaseModel
    {
        [PrimaryKey("LogId")]
        [Column("LogId", ignoreOnInsert: true)]
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
