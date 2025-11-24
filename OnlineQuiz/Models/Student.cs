using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Student")]
    public class Student
    {
        [Key]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("StudentId")]
        public string StudentId { get; set; } = string.Empty;

        [Column("Year_Level")]
        public int? YearLevel { get; set; }

        [MaxLength(100)]
        [Column("Section")]
        public string? Section { get; set; }

        [MaxLength(255)]
        [Column("Course")]
        public string? Course { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
    }
}
