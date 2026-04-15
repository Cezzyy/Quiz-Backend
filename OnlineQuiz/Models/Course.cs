using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Course")]
    public class Course : BaseModel
    {
        [PrimaryKey("CourseId")]
        [Column("CourseId", ignoreOnInsert: true)]
        public int CourseId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("Instructor_UserId")]
        public int InstructorUserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Active";

        [MaxLength(100)]
        [Column("Category")]
        public string? Category { get; set; }

        [MaxLength(100)]
        [Column("Section")]
        public string? Section { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("ArchivedAt")]
        public DateTime? ArchivedAt { get; set; }

        [Column("ArchivedBy")]
        public int? ArchivedBy { get; set; }
    }
}
