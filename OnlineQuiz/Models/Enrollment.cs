using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Enrollment")]
    public class Enrollment
    {
        [Key]
        [Column("EnrollmentId")]
        public int EnrollmentId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("CourseId")]
        public int CourseId { get; set; }

        [Column("EnrolledAt")]
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        [Column("Section")]
        public string? Section { get; set; }

        [Required]
        [Column("EnrolledBy")]
        public int EnrolledBy { get; set; }
    }
}
