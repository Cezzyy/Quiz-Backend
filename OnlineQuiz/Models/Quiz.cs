using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Quiz")]
    public class Quiz
    {
        [Key]
        [Column("QuizId")]
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

        // Navigation properties
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public Teacher CreatedByTeacher { get; set; } = null!;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
    }
}
