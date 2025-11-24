using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Teacher")]
    public class Teacher
    {
        [Key]
        [Column("UserId")]
        public int UserId { get; set; }

        [MaxLength(255)]
        [Column("Department")]
        public string? Department { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
