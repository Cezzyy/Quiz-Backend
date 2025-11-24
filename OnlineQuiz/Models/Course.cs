using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Course")]
    public class Course
    {
        [Key]
        [Column("CourseId")]
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

        // Navigation properties
        [ForeignKey("InstructorUserId")]
        public Teacher Instructor { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public User Creator { get; set; } = null!;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
