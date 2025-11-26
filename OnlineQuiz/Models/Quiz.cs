using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Quiz")]
    public class Quiz : BaseModel
    {
        [PrimaryKey("QuizId")]
        [Column("QuizId", ignoreOnInsert: true)]
        public int QuizId { get; set; }

        [Required]
        [Column("CourseId")]
        public int CourseId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        [Column("Due_At")]
        public DateTime? DueAt { get; set; }

        [Column("Time_Limit_Minutes")]
        public int? TimeLimitMinutes { get; set; }

        [Column("Is_Published")]
        public bool IsPublished { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("CreatedBy")]
        public int CreatedBy { get; set; }
    }
}
