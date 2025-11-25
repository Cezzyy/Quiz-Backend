using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class CreateExportImportLogDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // Export, Import

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
    }

    public class UpdateExportImportLogDto
    {
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Pending, In Progress, Completed, Failed

        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class ExportImportLogResponseDto
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
