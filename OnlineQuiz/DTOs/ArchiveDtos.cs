using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    /// <summary>
    /// DTO for archiving a single entity
    /// </summary>
    public class ArchiveDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ArchivedBy { get; set; }
    }

    /// <summary>
    /// DTO for bulk archiving multiple entities
    /// </summary>
    public class BulkArchiveDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one ID must be provided")]
        public List<int> Ids { get; set; } = new List<int>();

        [Required]
        public int ArchivedBy { get; set; }
    }

    /// <summary>
    /// DTO for unarchiving (restoring) a single entity
    /// </summary>
    public class UnarchiveDto
    {
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    /// DTO for bulk unarchiving multiple entities
    /// </summary>
    public class BulkUnarchiveDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one ID must be provided")]
        public List<int> Ids { get; set; } = new List<int>();
    }

    /// <summary>
    /// Response DTO for archive operations
    /// </summary>
    public class ArchiveResponseDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ArchivedAt { get; set; }
        public int? ArchivedBy { get; set; }
        public string? ArchivedByName { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response DTO for bulk archive operations
    /// </summary>
    public class BulkArchiveResponseDto
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<int> SuccessfulIds { get; set; } = new List<int>();
        public List<ArchiveErrorDto> Errors { get; set; } = new List<ArchiveErrorDto>();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error details for failed archive operations
    /// </summary>
    public class ArchiveErrorDto
    {
        public int Id { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for archive statistics
    /// </summary>
    public class ArchiveStatisticsDto
    {
        public string EntityType { get; set; } = string.Empty;
        public int ActiveCount { get; set; }
        public int ArchivedCount { get; set; }
        public int InactiveCount { get; set; }
        public int TotalCount { get; set; }
        public decimal ArchivePercentage { get; set; }
    }

    /// <summary>
    /// DTO for filtering archived items
    /// </summary>
    public class ArchiveFilterDto
    {
        /// <summary>
        /// Include archived items in results
        /// </summary>
        public bool IncludeArchived { get; set; } = false;

        /// <summary>
        /// Show only archived items
        /// </summary>
        public bool OnlyArchived { get; set; } = false;

        /// <summary>
        /// Filter by archived date range - start date
        /// </summary>
        public DateTime? ArchivedFrom { get; set; }

        /// <summary>
        /// Filter by archived date range - end date
        /// </summary>
        public DateTime? ArchivedTo { get; set; }

        /// <summary>
        /// Filter by who archived the items
        /// </summary>
        public int? ArchivedBy { get; set; }
    }

}
